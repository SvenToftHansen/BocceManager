# AI Handoff (Claude <-> Copilot)

Purpose: keep both assistants aligned on business rules, implementation approach, and current state.

Fast workflow (low memory):
1. Run task: Handoff: Copy Claude Prompt.
2. Paste into Claude and work.
3. Run task: Handoff: Copy Latest Context.
4. Paste into Copilot (or Claude next session).

How to use:
1. At the end of each work session, update the "Latest Handoff" section.
2. At the start of a new session, paste this handoff block into the assistant chat.
3. Do not delete old handoffs; move the previous one to "History".

## Latest Handoff

Date:
Owner:
Assistant: Claude | Copilot
Branch:

### 1) Goal Now
- 

### 2) Business Rules (Non-Negotiable)
- 

### 3) Decisions Already Made
- Chosen:
- Rejected:

### 4) Current State
- Done:
- In progress:
- Blocked:

### 5) Scope and Files
- Files changed:
- Files that must not change:

### 6) Validation
- Tests run:
- Tests still needed:
- Manual checks:

### 7) Risks / Unknowns
- 

### 8) Next Actions (Ordered)
1. 
2. 
3. 

## Copy/Paste Handoff Block

Use this block when switching assistants:

I am handing off from another assistant. Continue with the same approach.
Goal now:
Business rules:
Decisions already made (chosen/rejected):
Current state (done/in progress/blocked):
Files changed:
Files that must not change:
Validation done / still needed:
Risks or unknowns:
Next actions:

## History

### Handoff YYYY-MM-DD
- Move previous "Latest Handoff" content here before replacing it.
