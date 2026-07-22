"""Backward-compatible ASGI entry point for local `uvicorn main:app` commands."""

from app.main import app


__all__ = ["app"]
