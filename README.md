# Agent Setup — Real Estate Platform
# How to use these files with your AI coding agent

---

## FILE STRUCTURE

```
your-project/
├── AGENTS.md                    ← MASTER file — agent reads this first always
├── docs/
│   ├── SCHEMA.md                ← All tables, columns, enums, indexes
│   ├── DOMAIN_RULES.md          ← Business logic — what each module must do
│   ├── API_CONTRACT.md          ← All endpoints, request/response shapes
│   └── CODING_PATTERNS.md       ← Exact code patterns to copy
└── prompts/
    └── PROMPT_TEMPLATES.md      ← Ready-made prompts for each module
```

---

## SETUP STEPS

1. Copy all these files into your project root
2. Make sure your agent can read files from the project directory (context window / file indexing)
3. Start with Prompt 1 (Domain layer) from PROMPT_TEMPLATES.md
4. Build each module in order — each depends on the previous

---

## AGENT BEST PRACTICES

### Before every session
Tell the agent:
> "Read AGENTS.md and all files in /docs before writing any code."

This one line prevents 90% of architecture mistakes.

### When adding a new module
Use the exact prompt from PROMPT_TEMPLATES.md.
Do not improvise — the prompts are calibrated to produce the right patterns.

### When the agent drifts
If the agent writes business logic in a controller, or calls SaveChanges in a repo:
> "Stop. You've violated a rule in AGENTS.md. Re-read section 3 (Layer Rules)
>  and fix the code without changing the architecture."

### When you're unsure what to build next
> "Read AGENTS.md section 17 (Modules Build Order).
>  We have completed modules 1-3. Tell me what to build next and use
>  the corresponding prompt from prompts/PROMPT_TEMPLATES.md."

### Keeping context across sessions
Agents lose context between sessions. Start every new session with:
> "This is the Real Estate Platform backend built with ASP.NET Core 8.
>  Read AGENTS.md and all files in /docs to understand the full context.
>  We are currently working on: [MODULE NAME]. Here is what is done so far: [brief summary]"

---

## DO NOT MODIFY THESE FILES WITHOUT REASON

AGENTS.md, SCHEMA.md, DOMAIN_RULES.md, API_CONTRACT.md, and CODING_PATTERNS.md
are the ground truth for the agent. Changing them mid-build will confuse the agent
about what has already been built.

If you need to change a design decision:
1. Update the relevant .md file
2. Tell the agent: "docs/DOMAIN_RULES.md has been updated. Re-read it before continuing."
3. Ask the agent to identify what existing code needs to change

---

## WHAT EACH FILE DOES

| File | Purpose | Agent uses it when |
|---|---|---|
| AGENTS.md | Architecture law | Every single prompt |
| SCHEMA.md | Table/column names | Writing entities, repos, migrations |
| DOMAIN_RULES.md | Business logic | Writing service methods |
| API_CONTRACT.md | Routes and shapes | Writing controllers and DTOs |
| CODING_PATTERNS.md | Code templates | Writing any class |
| PROMPT_TEMPLATES.md | Ready prompts | Starting a new module |
