import json
import numpy as np
from pathlib import Path

_chunks: list[dict] = []
_embeddings: np.ndarray | None = None

EMBEDDINGS_FILE = Path(__file__).parent / "embeddings.json"
SIMILARITY_THRESHOLD = 0.35  # cosine similarity — chunks below this are considered irrelevant


def load():
    global _chunks, _embeddings
    if not EMBEDDINGS_FILE.exists():
        raise FileNotFoundError(
            "embeddings.json not found. Run: python seed/create_embeddings.py"
        )
    data = json.loads(EMBEDDINGS_FILE.read_text())
    _chunks = data
    _embeddings = np.array([c["embedding"] for c in data], dtype=np.float32)


def count() -> int:
    return len(_chunks)


def search(query_embedding: list[float], top_k: int = 5) -> list[dict]:
    if _embeddings is None or len(_chunks) == 0:
        return []

    q = np.array(query_embedding, dtype=np.float32)
    q_norm = q / (np.linalg.norm(q) + 1e-10)
    e_norms = _embeddings / (np.linalg.norm(_embeddings, axis=1, keepdims=True) + 1e-10)

    similarities = e_norms @ q_norm
    top_indices = np.argsort(similarities)[::-1][:top_k]

    results = []
    for idx in top_indices:
        if similarities[idx] >= SIMILARITY_THRESHOLD:
            results.append({
                "topic": _chunks[idx]["topic"],
                "content": _chunks[idx]["content"],
            })
    return results
