from contextlib import asynccontextmanager
from fastapi import FastAPI
import store
from router import router


@asynccontextmanager
async def lifespan(app: FastAPI):
    store.load()
    yield


app = FastAPI(title="EstateSync AI Service", lifespan=lifespan)
app.include_router(router)


@app.get("/health")
def health():
    # Readiness depends on the embedding store being loaded, not just the process being up.
    return {"status": "ok", "chunks": store.count()}
