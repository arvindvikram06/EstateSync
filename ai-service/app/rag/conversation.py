from collections.abc import Iterable


MAX_HISTORY_TURNS = 10
MAX_SEARCH_HISTORY_MESSAGES = 2


def bounded_history(history: Iterable[dict]) -> list[dict]:
    """Keep only valid, non-empty recent turns for model and retrieval context."""
    cleaned = []
    for turn in history:
        role = turn.get("role")
        content = str(turn.get("content", "")).strip()
        if role not in {"user", "assistant"} or not content:
            continue
        cleaned.append({"role": role, "content": content[:1000]})
    return cleaned[-MAX_HISTORY_TURNS:]


def semantic_search_query(message: str, history: Iterable[dict]) -> str:
    """Combine the latest user topics with the current question for embedding."""
    recent_user_messages = [
        turn["content"]
        for turn in bounded_history(history)
        if turn["role"] == "user"
    ][-MAX_SEARCH_HISTORY_MESSAGES:]
    parts = [f"Previous user message: {content}" for content in recent_user_messages]
    parts.append(f"Current question: {message.strip()}")
    return "\n".join(parts)
