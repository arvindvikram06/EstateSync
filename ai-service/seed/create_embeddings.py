"""
Run once to generate embeddings for all knowledge base chunks and save to embeddings.json.
Content lives in content.md — each '## Heading' section becomes one chunk.
Usage: python seed/create_embeddings.py  (from the ai-service/ directory)
"""
import sys
import os
import json
import re
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from embedder import embed_batch

CONTENT_FILE = Path(__file__).parent / "content.md"
OUTPUT = Path(__file__).parent.parent / "embeddings.json"


def parse_chunks(markdown: str) -> list[dict]:
    """Split markdown into chunks — one per '## Heading' section."""
    chunks = []
    # split on level-2 headings, keeping the heading text
    sections = re.split(r"^## ", markdown, flags=re.MULTILINE)
    for section in sections:
        section = section.strip()
        if not section:
            continue
        lines = section.split("\n", 1)
        topic = lines[0].strip()
        content = lines[1].strip() if len(lines) > 1 else ""
        if not content:
            continue
        chunk_id = re.sub(r"[^a-z0-9]+", "-", topic.lower()).strip("-")
        chunks.append({"chunk_id": chunk_id, "topic": topic, "content": content})
    return chunks


def main():
    markdown = CONTENT_FILE.read_text()
    chunks = parse_chunks(markdown)
    print(f"Parsed {len(chunks)} chunks from {CONTENT_FILE.name}.")

    # embed the heading + content together for better retrieval
    texts = [f"{c['topic']}\n{c['content']}" for c in chunks]
    embeddings = embed_batch(texts)

    data = []
    for chunk, embedding in zip(chunks, embeddings):
        data.append({
            "chunk_id": chunk["chunk_id"],
            "topic": chunk["topic"],
            "content": chunk["content"],
            "embedding": embedding,
        })

    OUTPUT.write_text(json.dumps(data, indent=2))
    print(f"Done. Saved {len(data)} chunks to {OUTPUT}")


if __name__ == "__main__":
    main()
