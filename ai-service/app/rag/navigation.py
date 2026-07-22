"""Resolve one safe navigation button from grounded evidence and trusted roles."""

from dataclasses import dataclass
from types import MappingProxyType
from typing import Iterable, Mapping, Sequence

from app.rag.retrieval import RetrievedChunk


@dataclass(frozen=True)
class NavigationAction:
    label: str
    route: str


@dataclass(frozen=True)
class _ActionVariant:
    label: str
    route: str
    roles: frozenset[str]


def _variant(label: str, route: str, *roles: str) -> _ActionVariant:
    return _ActionVariant(label=label, route=route, roles=frozenset(roles))


# Variant order is intentional. When an upgraded customer has both Owner and
# Tenant capabilities, owner-management destinations win for owner-side topics.
_ACTIONS: dict[str, tuple[_ActionVariant, ...]] = {
    "open-dashboard": (
        _variant("Open Admin Dashboard", "/admin/dashboard", "Admin"),
        _variant("Open Review Dashboard", "/verifier/dashboard", "Verifier"),
        _variant("Open Owner Dashboard", "/owner/dashboard", "Owner"),
        _variant("Open Dashboard", "/dashboard", "Tenant"),
    ),
    "open-profile": (
        _variant("Open Profile", "/profile", "Owner", "Tenant"),
    ),
    "view-kyc-status": (
        _variant("Review KYC", "/admin/dashboard?tab=kyc", "Admin"),
        _variant("Review KYC", "/verifier/dashboard?tab=kyc", "Verifier"),
        _variant("View KYC Status", "/dashboard?tab=verifications", "Owner", "Tenant"),
    ),
    "browse-properties": (
        _variant("Browse Properties", "/", "Owner", "Tenant"),
    ),
    "manage-properties": (
        _variant("Review Properties", "/admin/dashboard?tab=properties", "Admin"),
        _variant("Manage Properties", "/owner/dashboard?tab=properties", "Owner"),
    ),
    "open-site-visits": (
        _variant("Review Visits", "/owner/dashboard?tab=visits", "Owner"),
        _variant("View Visits", "/dashboard?tab=visits", "Tenant"),
    ),
    "open-applications": (
        _variant("Review Applications", "/owner/dashboard?tab=applications", "Owner"),
        _variant("View Applications", "/dashboard?tab=applications", "Tenant"),
    ),
    "open-leases": (
        _variant("Review Leases", "/admin/dashboard?tab=leases", "Admin"),
        _variant("Manage Leases", "/owner/dashboard?tab=leases", "Owner"),
        _variant("View Leases", "/dashboard?tab=leases", "Tenant"),
    ),
    "view-bills": (
        _variant("View Financials", "/admin/dashboard?tab=financial", "Admin"),
        _variant("View Bills", "/dashboard?tab=bills", "Tenant"),
    ),
    "view-earnings": (
        _variant("View Financials", "/admin/dashboard?tab=financial", "Admin"),
        _variant("View Earnings", "/owner/dashboard?tab=earnings", "Owner"),
    ),
    "open-complaints": (
        _variant("Review Complaints", "/admin/dashboard?tab=complaints", "Admin"),
        _variant("Manage Complaints", "/owner/dashboard?tab=complaints", "Owner"),
        _variant("View Complaints", "/dashboard?tab=complaints", "Tenant"),
    ),
    "open-review-dashboard": (
        _variant("Open Admin Dashboard", "/admin/dashboard", "Admin"),
        _variant("Open Review Dashboard", "/verifier/dashboard", "Verifier"),
    ),
}

ACTION_ALLOWLIST: Mapping[str, tuple[_ActionVariant, ...]] = MappingProxyType(_ACTIONS)


def _resolve_variant(action_id: str, roles: set[str]) -> NavigationAction | None:
    for variant in ACTION_ALLOWLIST.get(action_id, ()):
        if roles.intersection(role.casefold() for role in variant.roles):
            return NavigationAction(label=variant.label, route=variant.route)
    return None


def select_retrieved_navigation_action(
    retrieved_chunks: Sequence[RetrievedChunk],
    roles: Iterable[str],
) -> NavigationAction | None:
    """Choose one allowlisted action directly from ranked retrieval results."""
    trusted_roles = {role.casefold() for role in roles if role.strip()}
    if not trusted_roles:
        return None
    for chunk in retrieved_chunks:
        if not chunk.action_id:
            continue
        action = _resolve_variant(chunk.action_id, trusted_roles)
        if action is not None:
            return action
    return None
