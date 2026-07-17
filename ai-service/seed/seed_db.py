"""
Run once to embed all FAQ chunks and upsert them into the faq_chunks table.
Usage: python seed/seed_db.py  (from the ai-service/ directory)
"""
import sys
import os
import asyncio
import asyncpg

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from config import settings
from embedder import embed_batch
from chunks import FAQ_CHUNKS


async def seed():
    print(f"Connecting to database...")
    pool = await asyncpg.create_pool(settings.DATABASE_URL)

    async with pool.acquire() as conn:
        await conn.execute("CREATE EXTENSION IF NOT EXISTS vector")
        await conn.execute("""
            CREATE TABLE IF NOT EXISTS faq_chunks (
                chunk_id   TEXT         PRIMARY KEY,
                topic      TEXT         NOT NULL,
                content    TEXT         NOT NULL,
                metadata   JSONB        DEFAULT '{}',
                embedding  vector(1024) NOT NULL,
                created_at TIMESTAMPTZ  DEFAULT NOW()
            )
        """)
        await conn.execute("""
            CREATE INDEX IF NOT EXISTS faq_chunks_embedding_idx
            ON faq_chunks
            USING hnsw (embedding vector_cosine_ops)
            WITH (m = 16, ef_construction = 64)
        """)

    texts = [c["content"] for c in FAQ_CHUNKS]
    print(f"Embedding {len(texts)} chunks via Voyage AI...")
    embeddings = embed_batch(texts)

    async with pool.acquire() as conn:
        for chunk, embedding in zip(FAQ_CHUNKS, embeddings):
            embedding_str = "[" + ",".join(str(x) for x in embedding) + "]"
            await conn.execute(
                """
                INSERT INTO faq_chunks (chunk_id, topic, content, embedding)
                VALUES ($1, $2, $3, $4::vector)
                ON CONFLICT (chunk_id) DO UPDATE
                    SET topic = EXCLUDED.topic,
                        content = EXCLUDED.content,
                        embedding = EXCLUDED.embedding
                """,
                chunk["chunk_id"],
                chunk["topic"],
                chunk["content"],
                embedding_str,
            )
        count = await conn.fetchval("SELECT COUNT(*) FROM faq_chunks")

    await pool.close()
    print(f"Done. {count} chunks in faq_chunks table.")


if __name__ == "__main__":
    asyncio.run(seed())
