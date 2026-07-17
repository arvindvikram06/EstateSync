import voyageai
from config import settings

_client = voyageai.Client(api_key=settings.VOYAGE_API_KEY)
_MODEL = "voyage-3-lite"


def embed_text(text: str) -> list[float]:
    result = _client.embed([text], model=_MODEL, input_type="query")
    return result.embeddings[0]


def embed_batch(texts: list[str]) -> list[list[float]]:
    result = _client.embed(texts, model=_MODEL, input_type="document")
    return result.embeddings
