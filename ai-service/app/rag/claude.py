"""Claude gateway call used for final answer generation."""

import anthropic

from app.settings import settings


_client = anthropic.Anthropic(
    api_key=settings.ANTHROPIC_API_KEY,
    base_url=settings.BASE_URL,
)

def complete_answer(system: str, prompt: str) -> str:
    response = _client.messages.create(
        model=settings.ANSWER_MODEL,
        max_tokens=700,
        temperature=0,
        system=system,
        messages=[{"role": "user", "content": prompt}],
    )
    blocks = [block.text for block in response.content if block.type == "text"]
    if not blocks:
        raise ValueError("Answer model returned no text")
    return " ".join(blocks)
