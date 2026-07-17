"""
Run once to generate embeddings for all FAQ chunks and save to embeddings.json.
Usage: python seed/create_embeddings.py  (from the ai-service/ directory)
"""
import sys
import os
import json
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from embedder import embed_batch
from chunks import FAQ_CHUNKS

OUTPUT = Path(__file__).parent.parent / "embeddings.json"


def main():
    print(f"Embedding {len(FAQ_CHUNKS)} chunks via Voyage AI...")
    texts = [c["content"] for c in FAQ_CHUNKS]
    embeddings = embed_batch(texts)

    data = []
    for chunk, embedding in zip(FAQ_CHUNKS, embeddings):
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
