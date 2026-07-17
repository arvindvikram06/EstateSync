import asyncpg
from config import settings

_pool: asyncpg.Pool | None = None


async def create_pool() -> asyncpg.Pool:
    global _pool
    _pool = await asyncpg.create_pool(settings.DATABASE_URL)
    async with _pool.acquire() as conn:
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
    return _pool


async def close_pool():
    if _pool:
        await _pool.close()


def get_pool() -> asyncpg.Pool:
    return _pool
