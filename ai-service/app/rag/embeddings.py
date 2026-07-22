from fastembed import TextEmbedding

MODEL_NAME = "BAAI/bge-small-en-v1.5"

_model: TextEmbedding | None = None


def _get_model() -> TextEmbedding:
    global _model
    if _model is None:
        _model = TextEmbedding(MODEL_NAME)
    return _model


def embed_text(text: str) -> list[float]:
    vec = next(iter(_get_model().embed([text])))
    return vec.tolist()


def embed_batch(texts: list[str]) -> list[list[float]]:
    return [vec.tolist() for vec in _get_model().embed(texts)]
