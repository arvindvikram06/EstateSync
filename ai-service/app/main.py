from contextlib import asynccontextmanager
from fastapi import FastAPI
from app.api.chat import router
from app.rag.pipeline import load_pipeline, pipeline_chunk_count


@asynccontextmanager
async def lifespan(app: FastAPI):
    load_pipeline()
    yield


app = FastAPI(title="EstateSync AI Service", lifespan=lifespan)
app.include_router(router)


@app.get("/health")
def health():
    return {"status": "ok", "chunks": pipeline_chunk_count()}
