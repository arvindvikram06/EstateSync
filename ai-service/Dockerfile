FROM python:3.12-slim

WORKDIR /app

# fastembed downloads its ONNX model here; baking it into the image keeps the
# pod's startup offline and fast.
ENV FASTEMBED_CACHE_PATH=/app/.fastembed_cache \
    PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

# Downloads the embedding model and generates embeddings.json from content.md.
# Both are baked into the image, so the running pod needs no network for embeddings.
RUN python seed/create_embeddings.py

EXPOSE 8000

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
