"""Natural, documentation-grounded answer generation for the simple RAG flow."""

from collections.abc import Iterable, Sequence
from html import escape
from pathlib import Path
import re

from app.rag.conversation import bounded_history
from app.rag.retrieval import RetrievedChunk


NO_INFORMATION_REPLY = (
    "I couldn't find enough EstateSync information for that question. "
    "Please try asking about a specific platform feature."
)
INTERNAL_REFUSAL_REPLY = (
    "I can help with using EstateSync, but I can't provide internal technical "
    "or security implementation details."
)

_INTERNAL_REQUEST_RE = re.compile(
    r"\b(?:api|database|system prompt|secret key|authorization internals|"
    r"authentication internals|role assignment|permission check|"
    r"security control|infrastructure)\b",
    flags=re.IGNORECASE,
)

_SYSTEM_PROMPT_PATH = Path(__file__).parents[2] / "prompts" / "assistant_system.txt"
_SYSTEM_PROMPT = _SYSTEM_PROMPT_PATH.read_text(encoding="utf-8").strip()


def _prompt(
    message: str,
    history: Iterable[dict],
    chunks: Sequence[RetrievedChunk],
    action_label: str | None,
) -> str:
    turns = bounded_history(history)[-10:]
    conversation = "\n".join(
        f'<turn role="{turn["role"]}">{escape(turn["content"])}</turn>'
        for turn in turns
    ) or "<no_previous_turns />"
    documentation = "\n".join(
        f'<section title="{escape(chunk.title)}">{escape(chunk.content)}</section>'
        for chunk in chunks
    )
    navigation = (
        f"<available_navigation_button>{escape(action_label)}</available_navigation_button>"
        if action_label
        else "<no_navigation_button />"
    )
    return (
        f"<recent_conversation>\n{conversation}\n</recent_conversation>\n"
        f"<latest_message>{escape(message)}</latest_message>\n"
        f"<estatesync_documentation>\n{documentation}\n</estatesync_documentation>\n"
        f"{navigation}"
    )


def generate_answer(
    message: str,
    history: Iterable[dict],
    chunks: Sequence[RetrievedChunk],
    action_label: str | None = None,
) -> str:
    if not chunks:
        return NO_INFORMATION_REPLY
    from app.rag.claude import complete_answer

    answer = complete_answer(
        _SYSTEM_PROMPT,
        _prompt(message, history, chunks, action_label),
    ).strip().replace("**", "")
    return answer or NO_INFORMATION_REPLY


def is_internal_request(message: str) -> bool:
    return _INTERNAL_REQUEST_RE.search(message) is not None
