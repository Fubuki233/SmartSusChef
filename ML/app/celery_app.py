"""Celery application configuration for SmartSusChef async tasks."""

import os

from celery import Celery

REDIS_URL = os.getenv("CELERY_BROKER_URL", "redis://localhost:6379/0")

celery_app = Celery(
    "smartsuschef",
    broker=REDIS_URL,
    backend=REDIS_URL,
    include=["app.tasks.training_orchestrator"],
)

celery_app.conf.update(
    task_serializer="json",
    accept_content=["json"],
    result_serializer="json",
    timezone="UTC",
    enable_utc=True,
    task_track_started=True,
    result_expires=86400,  # 24 hours
)
