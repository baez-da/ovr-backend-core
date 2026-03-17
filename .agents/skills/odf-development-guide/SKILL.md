---
name: odf-development-guide
description: Use when designing, implementing, or reviewing any feature in the OVR system; when making domain modeling decisions; when unsure about ODF message structures, field semantics, or competition flow; when validating that implementation aligns with ODF standards; when working with bounded contexts defined in consolidated-architecture.md
---

# ODF as Published Language — Development Guide

This skill codifies how to use the ODF (Olympic Data Feed) standard as the foundational vocabulary for all domain
modeling and implementation decisions in this project.

**Reference architecture:** `consolidated-architecture.md` (root of the project).

## Core Philosophy

ODF is a **published language**, not an architectural prison. It defines the vocabulary of output — what can be emitted,
when, and under what conditions — but does not dictate internal implementation. Our system adopts ODF's invariants
without inheriting Olympic-scale institutional complexity.

### What ODF gives us

- A battle-tested domain vocabulary refined over decades of real Olympic operations.
- Message structures (`DT_RESULT`, `DT_ENTRIES`, `DT_PARTIC`, etc.) that encode real operational invariants.
- Field-level constraints that reveal business rules (e.g., `teamId` format `DDDGEEEEEEEENOCnn` encodes the rule that
  one team = one event).
- State machines validated by experts (e.g., `START_LIST → LIVE → UNOFFICIAL → OFFICIAL`).
- Extensibility via `/ExtendedDescription` key-value pairs and sport-specific Data Dictionaries.

### What ODF does NOT give us

- Internal architecture decisions (it doesn't care if we use microservices or a monolith).
- UI/UX specifications (those live in ORIS, which is restricted under NDA).
- Operator workflows (screen flows, button layouts, manual override procedures).
- Sport-specific business logic beyond what the message constraints imply.

### Our position

We don't have access to ORIS. We work from the **public ODF Data Dictionaries** and read them "in reverse" — if ODF
defines with precision what can be emitted, when, and under what conditions, then it also reveals the operational model
that must exist behind it.

## The Reverse-Reading Strategy

When facing a domain modeling question, follow this process:

```
1. Find the ODF message(s) that would carry this data
   → e.g., "How do entries work?" → look at DT_ENTRIES structure

2. Read the message structure, attributes, and constraints
   → What fields are mandatory? What are the valid states?
   → What triggers the message? What is the frequency?

3. Infer the domain rules from the output constraints
   → If DT_ENTRIES has EntryStatus per event, then entries are per-event, not per-person
   → If teamId embeds the event code, then teams are event-scoped

4. Validate against consolidated-architecture.md
   → Does this align with our bounded contexts?
   → Does it fit our existing design decisions?

5. Consult NotebookLM if available (see section below)
   → Cross-reference with loaded ODF sources for deeper context
```

## When to Consult NotebookLM

This project has ODF documentation loaded in a NotebookLM notebook that can be queried for domain validation.

**Notebook:** `ODF Language Guidelines and Participant Names OWG 2026`
**ID:** `86c2df5c-5884-4b53-abf8-2cf74f2fb876`

**Use the `nlm-skill` (NotebookLM skill) to consult when:**

- Designing a new feature and need to understand the ODF message structure it maps to.
- Unsure about field semantics, valid values, or constraints of an ODF element.
- Validating that an implementation decision aligns with ODF's model.
- Resolving ambiguity in the consolidated architecture document.
- Reviewing code that handles ODF message generation or parsing.
- Encountering a domain concept you haven't seen before (e.g., "generic events", "Initial Download").
- Making decisions about state machines, progression logic, or result lifecycle.

**Colloquial trigger phrases (team shorthand):**

- If the request includes phrasing like "pregunta al oraculo de odf", "pregunta al oraculo",
  "consulta al oraculo de odf", or similar, treat it as an explicit instruction to query the ODF NotebookLM notebook.
- If the phrase is ambiguous, default to asking 1 clarifying question and then proceed with the NotebookLM query.

**How to query:**

```bash
nlm notebook query 86c2df5c-5884-4b53-abf8-2cf74f2fb876 "your question here"
```

Use `--conversation-id` from previous queries to maintain context in follow-up questions.

**Example queries:**

- "What fields are mandatory in a DT_RESULT Competitor element when Type is T (team)?"
- "How does DT_POOL_STANDING handle tiebreakers in group stages?"
- "What triggers a DT_SCHEDULE_UPDATE message?"
- "What is the relationship between DT_MEDALLISTS and DT_MEDALS?"

## Decision Framework

### When to follow ODF strictly

- **Message output format:** If we emit ODF messages, they must conform to the Data Dictionary. No negotiation.
- **Identifier formats:** `teamId`, participant codes, RSC codes — these are shared vocabulary with external systems.
- **State machines in messages:** If ODF defines states for a message (e.g., result status), our internal states must
  map cleanly to them.
- **Semantic invariants:** One team per event, entries per event not per person, function as attribute of assignment not
  of person — these are domain truths, not ODF quirks.

### When to adapt or simplify

- **Institutional workflows:** We don't need the full Olympic ceremony protocol. We need the result lifecycle.
- **Scale-specific features:** Anti-doping integration, multi-venue coordination, broadcast graphics injection — only
  if the project scope demands them.
- **Historical compatibility:** ODF carries legacy fields for backward compatibility. We don't need to support fields
  marked as deprecated or irrelevant to our scope.
- **Sport coverage:** We implement sport-specific rules incrementally. Start with the generic Data Dictionary, add
  sport-specific extensions as needed.

### When Olympic rigor does NOT apply

- Semi-professional or regional events may not have a GMS/SEQ. The OVR can operate standalone with local participant
  creation (`LOC-*` IDs).
- Not every event needs the full Initial Download ceremony. Entries can be created directly in the OVR.
- The transfer of control concept still applies conceptually (someone owns the data at any given time), but the
  formal 3-5 day window and SEQ coordination may not exist.
- Message distribution to ODR/external consumers is optional. The core domain model is valid even without downstream
  distribution.

## Bounded Context Alignment

Every implementation decision should be traceable to a bounded context in `consolidated-architecture.md`:

| Domain question | Bounded context | ODF message reference |
|----------------|----------------|----------------------|
| Who is this person? | Participant Registry | `DT_PARTIC` |
| What team are they on? | Team Composition | `DT_PARTIC_TEAMS` |
| What event structure exists? | Competition Config | `DT_SCHEDULE` |
| When does each unit happen? | Scheduling | `DT_SCHEDULE`, `DT_SCHEDULE_UPDATE` |
| Who is entered in what event? | Entries | `DT_ENTRIES` |
| Who officiates this unit? | Official Assignment | (within `DT_RESULT` officials section) |
| What happened in competition? | Data Entry | `DT_RESULT`, `DT_PLAY_BY_PLAY`, `DT_STATS` |
| Who advances to next phase? | Progression | `DT_POOL_STANDING`, `DT_BRACKETS` |
| What gets sent externally? | Data Distribution | All `DT_*` messages |

## Checklist for Feature Implementation

Before implementing any feature that touches the domain model:

- [ ] Identify which bounded context(s) this feature belongs to.
- [ ] Find the ODF message(s) that would carry or be affected by this data.
- [ ] Verify alignment with `consolidated-architecture.md`.
- [ ] If uncertain about ODF semantics, consult NotebookLM with specific questions.
- [ ] Confirm that the feature respects the "separate identity from assignment" principle.
- [ ] Ensure internal state machines can map cleanly to ODF output states.
- [ ] Check if this is a domain invariant (follow strictly) or an institutional workflow (adapt to our scope).
