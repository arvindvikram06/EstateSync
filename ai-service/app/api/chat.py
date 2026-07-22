import logging
from typing import Literal

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel, Field
from app.rag.conversation import bounded_history
from app.rag.pipeline import answer_question
from starlette.concurrency import run_in_threadpool

logger = logging.getLogger(__name__)

_SERVICE_ERROR_REPLY = (
    "The EstateSync assistant is temporarily unavailable. Please try again shortly."
)

router = APIRouter()


class HistoryTurn(BaseModel):
    role: Literal["user", "assistant"]
    content: str = Field(min_length=1, max_length=1000)


class ChatRequest(BaseModel):
    message: str = Field(min_length=1, max_length=500)
    roles: list[str] = Field(default_factory=list)
    history: list[HistoryTurn] = Field(default_factory=list, max_length=10)


class Source(BaseModel):
    title: str
    chunkId: str
    documentVersion: str


class NavigationAction(BaseModel):
    label: str
    route: str


class ChatResponse(BaseModel):
    reply: str
    sources: list[Source] = Field(default_factory=list)
    actions: list[NavigationAction] = Field(default_factory=list)


@router.post("/chat", response_model=ChatResponse)
async def chat(request: ChatRequest):
    if not request.message.strip():
        raise HTTPException(status_code=400, detail="Message cannot be empty")

    history = bounded_history([turn.model_dump() for turn in request.history])
    try:
        result = await run_in_threadpool(
            answer_question,
            request.message.strip(),
            request.roles,
            history,
        )
    except Exception:
        logger.exception("EstateSync AI pipeline failed")
        return ChatResponse(reply=_SERVICE_ERROR_REPLY)

    sources = [
        Source(
            title=source.title,
            chunkId=source.chunk_id,
            documentVersion=source.version,
        )
        for source in result.sources
    ]
    actions = (
        [NavigationAction(label=result.action.label, route=result.action.route)]
        if result.action is not None
        else []
    )
    return ChatResponse(reply=result.reply, sources=sources, actions=actions)
