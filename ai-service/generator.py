from pathlib import Path

import anthropic
from config import settings

_client = anthropic.Anthropic(api_key=settings.ANTHROPIC_API_KEY, base_url=settings.BASE_URL)
_MODEL = "claude-sonnet-4-6"

_PROMPT_DIR = Path(__file__).parent / "prompts"
_SYSTEM = (_PROMPT_DIR / "system.txt").read_text(encoding="utf-8").strip()
_OFFTOPIC_SYSTEM = (_PROMPT_DIR / "offtopic.txt").read_text(encoding="utf-8").strip()


def generate_offtopic(user_message: str) -> str:
    """Redirect when no knowledge-base chunk matched.

    No context is passed, so there is no platform data available to leak here.
    """
    message = _client.messages.create(
        model=_MODEL,
        max_tokens=150,
        system=_OFFTOPIC_SYSTEM,
        messages=[
            {
                "role": "user",
                "content": f"<user_message>\n{user_message}\n</user_message>",
            }
        ],
    )
    return message.content[0].text


def generate(user_message: str, context_chunks: list[dict]) -> str:
    context_text = "\n\n".join(
        f"[{c['topic']}]\n{c['content']}" for c in context_chunks
    )
    message = _client.messages.create(
        model=_MODEL,
        max_tokens=512,
        system=_SYSTEM,
        messages=[
            {
                "role": "user",
                "content": f"<context>\n{context_text}\n</context>\n\n{user_message}",
            }
        ],
    )
    return message.content[0].text
