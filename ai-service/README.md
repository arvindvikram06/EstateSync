# EstateSync AI service

The service answers EstateSync product questions with simple semantic RAG and returns
an optional allowlisted navigation action.

## Structure

```text
app/
  api/chat.py          HTTP request and response contract
  knowledge/           Markdown ingestion and index construction
  rag/                 conversation, retrieval, generation and navigation
  main.py              FastAPI startup and health endpoint
  settings.py          environment and secret configuration
docs/                  versioned user-facing product documentation
prompts/               Claude system instructions
scripts/build_index.py builds knowledge_index.json
```

## Request flow

```text
question + recent messages
  -> one history-aware search string
  -> role-filtered semantic retrieval (five sections)
  -> documentation-grounded Claude answer
  -> allowlisted navigation action
```

## Local commands

```bash
.venv/bin/pip install -r requirements.txt
.venv/bin/python scripts/build_index.py --force
.venv/bin/uvicorn app.main:app --reload --host 127.0.0.1 --port 8000
```
