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

# Download the embedding model and build the versioned knowledge index. Both are
# baked into the image, so startup does not need to ingest documents or download it.
RUN python scripts/build_index.py

EXPOSE 8000

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
