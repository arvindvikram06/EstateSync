"""Build the metadata-rich embedding index used by the new RAG pipeline."""

from collections.abc import Callable
import hashlib
import json
from pathlib import Path

from app.rag.embeddings import MODEL_NAME, embed_batch
from app.knowledge.ingestion import (
    DEFAULT_MAX_WORDS,
    DEFAULT_OVERLAP_WORDS,
    DocumentChunk,
    ingest_directory,
)


INDEX_SCHEMA_VERSION = "1.0"

EmbeddingFunction = Callable[[list[str]], list[list[float]]]


def calculate_content_hash(
    docs_directory: Path,
    max_words: int = DEFAULT_MAX_WORDS,
    overlap_words: int = DEFAULT_OVERLAP_WORDS,
) -> str:
    """Hash documents and indexing settings so stale indexes are detectable."""
    digest = hashlib.sha256()
    digest.update(f"schema={INDEX_SCHEMA_VERSION}\n".encode())
    digest.update(f"model={MODEL_NAME}\n".encode())
    digest.update(f"max_words={max_words}\n".encode())
    digest.update(f"overlap_words={overlap_words}\n".encode())

    for path in sorted(docs_directory.glob("*.md")):
        digest.update(path.name.encode("utf-8"))
        digest.update(b"\0")
        digest.update(path.read_bytes())
        digest.update(b"\0")
    return digest.hexdigest()


def embedding_text(chunk: DocumentChunk) -> str:
    """Only user-facing title/content are embedded; security metadata is not."""
    return f"{chunk.title}\n{chunk.content}"


def _serialize_chunk(chunk: DocumentChunk, embedding: list[float]) -> dict:
    return {
        "id": chunk.chunk_id,
        "text": chunk.content,
        "embedding": embedding,
        "metadata": {
            "document_id": chunk.document_id,
            "title": chunk.title,
            "version": chunk.version,
            "roles": list(chunk.roles),
            "category": chunk.category,
            "action_id": chunk.action_id,
            "source": Path(chunk.source_path).name,
            "chunk_index": chunk.chunk_index,
        },
    }


def _existing_index_is_current(output: Path, content_hash: str) -> bool:
    if not output.exists():
        return False
    try:
        data = json.loads(output.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError):
        return False
    return (
        data.get("schema_version") == INDEX_SCHEMA_VERSION
        and data.get("content_hash") == content_hash
        and data.get("embedding_model") == MODEL_NAME
    )


def build_index(
    docs_directory: Path,
    output: Path,
    *,
    max_words: int = DEFAULT_MAX_WORDS,
    overlap_words: int = DEFAULT_OVERLAP_WORDS,
    embedding_function: EmbeddingFunction = embed_batch,
    force: bool = False,
) -> dict:
    """Validate documents, embed chunks, and atomically write the index."""
    content_hash = calculate_content_hash(docs_directory, max_words, overlap_words)
    if not force and _existing_index_is_current(output, content_hash):
        return json.loads(output.read_text(encoding="utf-8"))

    chunks = ingest_directory(docs_directory, max_words, overlap_words)
    texts = [embedding_text(chunk) for chunk in chunks]
    embeddings = embedding_function(texts)

    if len(embeddings) != len(chunks):
        raise ValueError(
            f"Embedding count mismatch: expected {len(chunks)}, got {len(embeddings)}"
        )
    dimensions = len(embeddings[0]) if embeddings else 0
    if dimensions == 0 or any(len(vector) != dimensions for vector in embeddings):
        raise ValueError("Embeddings must be non-empty vectors with equal dimensions")

    document_count = len({chunk.document_id for chunk in chunks})
    index = {
        "schema_version": INDEX_SCHEMA_VERSION,
        "content_hash": content_hash,
        "embedding_model": MODEL_NAME,
        "dimensions": dimensions,
        "chunking": {
            "strategy": "overlapping-words",
            "max_words": max_words,
            "overlap_words": overlap_words,
        },
        "document_count": document_count,
        "chunk_count": len(chunks),
        "chunks": [
            _serialize_chunk(chunk, embedding)
            for chunk, embedding in zip(chunks, embeddings, strict=True)
        ],
    }

    output.parent.mkdir(parents=True, exist_ok=True)
    temporary_output = output.with_suffix(f"{output.suffix}.tmp")
    temporary_output.write_text(json.dumps(index, indent=2), encoding="utf-8")
    temporary_output.replace(output)
    return index
