"""Orchestrate the complete EstateSync support RAG pipeline."""

from dataclasses import dataclass
from pathlib import Path

from app.rag.conversation import bounded_history, semantic_search_query
from app.rag.generation import INTERNAL_REFUSAL_REPLY, generate_answer, is_internal_request
from app.rag.navigation import NavigationAction, select_retrieved_navigation_action
from app.rag import retrieval
from app.rag.retrieval import RetrievedChunk


INDEX_PATH = Path(__file__).parents[2] / "knowledge_index.json"


@dataclass(frozen=True)
class PipelineResult:
    reply: str
    sources: tuple[RetrievedChunk, ...]
    action: NavigationAction | None


def load_pipeline() -> None:
    retrieval.load(INDEX_PATH)


def pipeline_chunk_count() -> int:
    return retrieval.count()


def answer_question(
    message: str,
    roles: list[str],
    history: list[dict],
) -> PipelineResult:
    if retrieval.count() == 0:
        raise RuntimeError("AI service is not loaded")

    trusted_roles = [role for role in roles if role.strip()]
    safe_history = bounded_history(history)

    if is_internal_request(message):
        return PipelineResult(
            reply=INTERNAL_REFUSAL_REPLY,
            sources=(),
            action=None,
        )

    search_query = semantic_search_query(message, safe_history)
    chunks = retrieval.retrieve(search_query, trusted_roles)
    action = select_retrieved_navigation_action(chunks, trusted_roles)
    reply = generate_answer(
        message=message.strip(),
        history=safe_history,
        chunks=chunks,
        action_label=action.label if action else None,
    )
    return PipelineResult(reply=reply, sources=tuple(chunks), action=action)
