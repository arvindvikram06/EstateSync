import os
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    # extra="ignore" so unrelated vars in the environment can't break startup
    model_config = SettingsConfigDict(
        env_file=".env", env_file_encoding="utf-8", extra="ignore"
    )

    ANTHROPIC_API_KEY: str
    BASE_URL: str


def _load_from_key_vault(vault_uri: str) -> dict:
    from azure.identity import DefaultAzureCredential
    from azure.keyvault.secrets import SecretClient

    client = SecretClient(vault_url=vault_uri, credential=DefaultAzureCredential())
    return {
        "ANTHROPIC_API_KEY": client.get_secret("Anthropic--ApiKey").value,
        "BASE_URL": client.get_secret("Anthropic--BaseUrl").value,
    }


# In AKS the pod's workload identity reads the vault; locally we fall back to .env.
_vault_uri = os.getenv("KEY_VAULT_URI")
settings = Settings(**_load_from_key_vault(_vault_uri)) if _vault_uri else Settings()
