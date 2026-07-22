"""Build knowledge_index.json from the versioned documents in ai-service/docs."""

import os
from pathlib import Path
import sys


AI_SERVICE_DIRECTORY = Path(__file__).resolve().parent.parent
sys.path.insert(0, os.fspath(AI_SERVICE_DIRECTORY))

from app.knowledge.indexer import build_index  # noqa: E402


def main() -> None:
    index = build_index(
        AI_SERVICE_DIRECTORY / "docs",
        AI_SERVICE_DIRECTORY / "knowledge_index.json",
        force="--force" in sys.argv,
    )
    print(
        "Knowledge index ready: "
        f"{index['document_count']} documents, "
        f"{index['chunk_count']} chunks, "
        f"{index['dimensions']} dimensions."
    )


if __name__ == "__main__":
    main()
