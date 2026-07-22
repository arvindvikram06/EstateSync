"""Role-filtered semantic retrieval for the EstateSync knowledge index."""

from dataclasses import dataclass
import json
from pathlib import Path

import numpy as np
from sklearn.metrics.pairwise import cosine_similarity

from app.knowledge.indexer import INDEX_SCHEMA_VERSION
from app.rag.embeddings import MODEL_NAME, embed_text


DEFAULT_TOP_K = 5
DEFAULT_MIN_SCORE = 0.45

@dataclass(frozen=True)
class RetrievedChunk:
    chunk_id: str
    title: str
    version: str
    action_id: str | None
    content: str


_chunks: list[dict] = []
_embeddings = np.empty((0, 0), dtype=np.float32)
_dimensions = 0


def load(index_path: Path) -> None:
    global _chunks, _embeddings, _dimensions
    index = json.loads(index_path.read_text(encoding="utf-8"))
    if index.get("schema_version") != INDEX_SCHEMA_VERSION:
        raise ValueError("Knowledge index schema is incompatible")
    if index.get("embedding_model") != MODEL_NAME:
        raise ValueError("Knowledge index embedding model is incompatible")
    _chunks = index["chunks"]
    _dimensions = index["dimensions"]
    _embeddings = np.asarray(
        [chunk["embedding"] for chunk in _chunks],
        dtype=np.float32,
    )


def count() -> int:
    return len(_chunks)


def retrieve(query: str, roles: list[str], top_k: int = DEFAULT_TOP_K) -> list[RetrievedChunk]:
    allowed = [
        index for index, chunk in enumerate(_chunks)
        if set(roles).intersection(chunk["metadata"]["roles"])
    ]
    if not query.strip() or not allowed:
        return []

    query_vector = np.asarray(embed_text(query), dtype=np.float32)
    if query_vector.size != _dimensions:
        raise ValueError("Query embedding has an unexpected size")

    scores = cosine_similarity(query_vector.reshape(1, -1), _embeddings)[0]
    ranked = sorted(allowed, key=lambda index: scores[index], reverse=True)[:top_k]

    results = []
    for index in ranked:
        if scores[index] < DEFAULT_MIN_SCORE:
            continue
        chunk = _chunks[index]
        metadata = chunk["metadata"]
        results.append(RetrievedChunk(
            chunk_id=chunk["id"],
            title=metadata["title"],
            version=metadata["version"],
            action_id=metadata.get("action_id"),
            content=chunk["text"],
        ))
    return results
