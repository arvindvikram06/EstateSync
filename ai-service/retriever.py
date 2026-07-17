import store


def retrieve(query_embedding: list[float], top_k: int = 5) -> list[dict]:
    return store.search(query_embedding, top_k)
