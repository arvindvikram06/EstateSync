from fastembed import TextEmbedding

_model = TextEmbedding("BAAI/bge-small-en-v1.5")


def embed_text(text: str) -> list[float]:
    vec = next(iter(_model.embed([text])))
    return vec.tolist()


def embed_batch(texts: list[str]) -> list[list[float]]:
    return [vec.tolist() for vec in _model.embed(texts)]
