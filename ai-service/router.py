import logging

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel
from embedder import embed_text
from retriever import retrieve
from generator import generate, generate_offtopic

logger = logging.getLogger(__name__)

_FALLBACK_REPLY = (
    "I can only answer questions about the EstateSync platform. "
    "Please ask me about features like KYC, leases, payments, or complaints."
)

router = APIRouter()


class ChatRequest(BaseModel):
    message: str


class ChatResponse(BaseModel):
    reply: str


@router.post("/chat", response_model=ChatResponse)
async def chat(request: ChatRequest):
    if not request.message.strip():
        raise HTTPException(status_code=400, detail="Message cannot be empty")

    query_embedding = embed_text(request.message)
    chunks = retrieve(query_embedding)

    if not chunks:
        # Nothing matched — redirect without passing any knowledge-base context,
        # so there is no platform data available to leak on this path.
        try:
            return ChatResponse(reply=generate_offtopic(request.message))
        except Exception:
            logger.exception("Off-topic redirect failed; using static reply")
            return ChatResponse(reply=_FALLBACK_REPLY)

    reply = generate(request.message, chunks)
    return ChatResponse(reply=reply)
