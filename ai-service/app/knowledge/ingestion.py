"""Parse versioned EstateSync Markdown documents into bounded RAG chunks.

This module deliberately does not create embeddings. Ingestion and embedding are
separate stages, which keeps document validation easy to test without loading an
embedding model.
"""

from dataclasses import dataclass
from pathlib import Path
import re


REQUIRED_METADATA = {"title", "version", "roles", "category"}
DEFAULT_MAX_WORDS = 220
DEFAULT_OVERLAP_WORDS = 40


@dataclass(frozen=True)
class SourceDocument:
    document_id: str
    title: str
    version: str
    roles: tuple[str, ...]
    category: str
    action_id: str | None
    content: str
    source_path: str


@dataclass(frozen=True)
class DocumentChunk:
    chunk_id: str
    document_id: str
    title: str
    version: str
    roles: tuple[str, ...]
    category: str
    action_id: str | None
    content: str
    source_path: str
    chunk_index: int


class DocumentFormatError(ValueError):
    """Raised when a knowledge document has invalid or missing metadata."""


def _parse_roles(value: str) -> tuple[str, ...]:
    normalized = value.strip().removeprefix("[").removesuffix("]")
    roles = tuple(
        role.strip().strip("'\"")
        for role in normalized.split(",")
        if role.strip().strip("'\"")
    )
    if not roles:
        raise DocumentFormatError("'roles' must contain at least one role")
    return roles


def _parse_frontmatter(markdown: str, source: Path) -> tuple[dict[str, str], str]:
    if not markdown.startswith("---"):
        raise DocumentFormatError(f"{source.name}: missing opening '---' frontmatter")

    parts = re.split(r"^---\s*$", markdown, maxsplit=2, flags=re.MULTILINE)
    if len(parts) != 3:
        raise DocumentFormatError(f"{source.name}: missing closing '---' frontmatter")

    metadata: dict[str, str] = {}
    for line_number, raw_line in enumerate(parts[1].splitlines(), start=2):
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue
        if ":" not in line:
            raise DocumentFormatError(
                f"{source.name}:{line_number}: expected 'key: value' metadata"
            )
        key, value = line.split(":", 1)
        metadata[key.strip().lower().replace("-", "_")] = value.strip().strip("'\"")

    missing = REQUIRED_METADATA.difference(metadata)
    if missing:
        names = ", ".join(sorted(missing))
        raise DocumentFormatError(f"{source.name}: missing metadata: {names}")

    content = parts[2].strip()
    if not content:
        raise DocumentFormatError(f"{source.name}: document content cannot be empty")
    return metadata, content


def parse_document(path: Path) -> SourceDocument:
    """Read and validate one versioned Markdown knowledge document."""
    metadata, content = _parse_frontmatter(path.read_text(encoding="utf-8"), path)
    document_id = re.sub(r"[^a-z0-9]+", "-", path.stem.lower()).strip("-")
    if not document_id:
        raise DocumentFormatError(f"{path.name}: filename must create a valid document id")

    return SourceDocument(
        document_id=document_id,
        title=metadata["title"],
        version=metadata["version"],
        roles=_parse_roles(metadata["roles"]),
        category=metadata["category"],
        action_id=metadata.get("action_id") or None,
        content=content,
        source_path=path.as_posix(),
    )


def chunk_document(
    document: SourceDocument,
    max_words: int = DEFAULT_MAX_WORDS,
    overlap_words: int = DEFAULT_OVERLAP_WORDS,
) -> list[DocumentChunk]:
    """Split a document into deterministic overlapping word-bounded chunks."""
    if max_words <= 0:
        raise ValueError("max_words must be greater than zero")
    if overlap_words < 0 or overlap_words >= max_words:
        raise ValueError("overlap_words must be between zero and max_words - 1")

    chunks: list[DocumentChunk] = []
    # Preserve Markdown topic boundaries first. Mixing the end of one workflow
    # with the beginning of another creates misleading retrieval matches.
    sections = [
        section.strip()
        for section in re.split(
            r"(?=^#{1,6}\s+)",
            document.content,
            flags=re.MULTILINE,
        )
        if section.strip()
    ]

    for section in sections:
        words = section.split()
        start = 0
        while start < len(words):
            end = min(start + max_words, len(words))
            chunk_index = len(chunks)
            chunks.append(DocumentChunk(
                chunk_id=f"{document.document_id}-{chunk_index:03d}",
                document_id=document.document_id,
                title=document.title,
                version=document.version,
                roles=document.roles,
                category=document.category,
                action_id=document.action_id,
                content=" ".join(words[start:end]),
                source_path=document.source_path,
                chunk_index=chunk_index,
            ))
            if end == len(words):
                break
            start = end - overlap_words

    return chunks


def ingest_directory(
    docs_directory: Path,
    max_words: int = DEFAULT_MAX_WORDS,
    overlap_words: int = DEFAULT_OVERLAP_WORDS,
) -> list[DocumentChunk]:
    """Parse every Markdown file in a directory into a stable chunk collection."""
    if not docs_directory.is_dir():
        raise FileNotFoundError(f"Knowledge directory not found: {docs_directory}")

    paths = sorted(docs_directory.glob("*.md"))
    if not paths:
        raise DocumentFormatError(f"No Markdown documents found in {docs_directory}")

    chunks: list[DocumentChunk] = []
    seen_ids: set[str] = set()
    for path in paths:
        document = parse_document(path)
        if document.document_id in seen_ids:
            raise DocumentFormatError(f"Duplicate document id: {document.document_id}")
        seen_ids.add(document.document_id)
        chunks.extend(chunk_document(document, max_words, overlap_words))
    return chunks
