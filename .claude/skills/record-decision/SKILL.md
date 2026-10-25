---
name: record-decision
description: Record a design decision or data-ambiguity resolution in the README's decisions section. Use whenever the rules don't settle a behaviour, a trade-off is made, or a dependency is added.
---

# Record a decision

Decisions live in the README, in "What the data didn't settle, and what I decided". There are two tables:
- **Data decisions:** how the data is interpreted (A-numbers, or a D-number for later refinements).
- **Design decisions referenced in the code:** structural choices the code comments point to.

1. **Propose it first.** Describe the case (cite record ids, e.g. `re_0004`), 2–3 options, and the behaviour you recommend,
   with the organiser-facing reason first. Only add it once the user has agreed.
2. **Add one row** to the right table: the next free id, what in the data or design prompted it, and the decision in one or
   two sentences. To change a decision, edit its row and say what it replaced. Don't keep two rows that contradict each other.
3. **Reference the id in the code comment** where the behaviour is implemented, e.g. `// (decision D-<id>)`.
4. **Pin it with a test** whose name describes the behaviour, so the decision can't drift silently.
5. If it changes a golden figure, update the derivation table in the README's "How I know the numbers are right"
   and `GoldenFigures.cs` together, and only with the user's explicit approval.
