# Engineering Acceleration with VS Code + Copilot

## Setup Context (30 sec)

**Goal:** Show how we accelerate engineering work with VS Code + Copilot, governed by instructions, powered by agents + MCP tools, and measured via PR reporting.

## Installing Copilot

### Confirm your v- Copilot access is enabled

- Go to [aka.ms/copilot](https://aka.ms/copilot) (internal onboarding) and complete the enablement steps for your Enterprise Managed User (EMU) account
- When signing into GitHub, use your EMU username (example pattern: `v-<alias>_microsoft`), not your email address

### 2. Visual Studio Code (most common)

- **Install the extensions:** GitHub Copilot and GitHub Copilot Chat in VS Code (Extensions view, search "GitHub Copilot", Install both)
- **Sign in to GitHub using your enterprise account:**
  - Go to [copilot.github.microsoft.com](https://copilot.github.microsoft.com/), select "Sign in to your enterprise account", then Continue
- **Back in VS Code:** Select "Sign in with GitHub to use GitHub Copilot", then complete the browser authorization ("Authorize Visual Studio Code")

## 1. VS Code Chat Settings (3 min)

Focus on what changed that matters for AI-assisted engineering:

- Todo
- Agent Sessions
- Thinking
- Skills
- YOLO
- Max Requests

## Copilot Instructions (5 min)

You want to show how we get repeatable results (and keep control).

**Reference:** [GitHub - Awesome Copilot](https://github.com/github/awesome-copilot) - Community-contributed instructions, prompts, and configurations.

### A. Repository-wide instructions

- Use `.github/copilot-instructions.md` for global repo guardrails

### B. Path-specific instructions

- Use `.github/instructions/*.instructions.md` with globs to scope by folder/file types

### Your recommended "instruction pattern" to demo

Put this in `.github/copilot-instructions.md`:

```text
- Default to PowerShell for terminal commands.
- Do not change code until you present a plan and I approve.
- When generating code, also propose tests and validation steps.
- When you use MCP tools, state which tool you will call and why.
- For PR descriptions, include an AI usage summary (model, approximate AI contribution, tests created).
```

This aligns with the "plan first, then execute" behaviors your team already discusses.

**References:**

- GitHub instructions guidance: Adding repository custom instructions
- VS Code guidance: Use custom instructions in VS Code

## What is an Agent (2 min)

Use this definition (simple and defensible):

**Agent mode** is an editing capability where Copilot can search your codebase, read relevant files, propose edits across multiple files, run commands (with confirmation), and iterate based on results.

## Plan Mode vs Agent Mode (4 min)

Use this framing:

### Plan Mode

- Produces a structured, trackable plan before execution
- In Visual Studio, "Planning in agent mode" explicitly creates a user-facing markdown plan and tracks steps

### Agent Mode

- Executes the steps: reads files, proposes multi-file edits, runs commands/tests, and iterates

### Practical demo move

1. **Start in Plan Mode:** "Outline the steps, list files you will touch, and stop."
2. **Then switch to Agent Mode:** "Proceed with step 1 only."

This matches the "assess, plan, then get approval" guidance your org already promotes.

## Your Recommended LLM (2 min)

You have concrete evidence in your own PR traffic. Your PR notifications repeatedly show **Model Used: Claude Sonnet 4.5** in the Copilot usage summaries.

So your "recommended LLM" for the demo can be:

- **Claude Sonnet 4.5** for coding + tests (because it is already being used and reported in your PR workflow)
- **GPT 5.2** when creating work items, ADRs, Documentation, or other non-coding artifacts (because it has strong capabilities for these tasks)

## MCP Servers We Use (6 min)

Make this very concrete: "MCP is how we give the agent tools."

**Reference:** [Model Context Protocol Servers](https://github.com/modelcontextprotocol/servers)

### A. Azure DevOps MCP server

- Manage work items, pull requests, builds, pipelines, wikis, and test plans
- **Demo action ideas:**
  1. "List my work items"
  2. "Get work item ####"
  3. "Create a pull request"
  4. "Add a wiki page documenting this feature"

### B. Azure MCP server

- Comprehensive Azure resource management and operations
- Generate Azure CLI commands, Bicep/Terraform best practices
- Deploy and monitor Azure resources (Container Registry, AKS, App Service, Functions, etc.)
- **Demo action ideas:**
  1. "Show me Azure Functions best practices"
  2. "Generate Bicep code for an Azure App Service"
  3. "List my Azure subscriptions"

### C. Microsoft Learn/Docs MCP

- Search official Microsoft and Azure documentation
- Retrieve code samples and implementation examples
- Fetch complete documentation pages
- **Demo action ideas:**
  1. "Find documentation on Entity Framework Core"
  2. "Show me C# code examples for async/await"

### D. Figma MCP server

- Extract UI code from Figma designs
- Map Figma components to code
- Generate diagrams (flowcharts, sequence diagrams, Gantt charts)
- **Demo action ideas:**
  1. "Generate a flowchart for the user registration process"
  2. "Create a sequence diagram showing the API call flow"

### E. S360 MCP server

- Manage KPI metadata and action items
- Track performance metrics and exceptions
- **Demo action ideas:**
  1. "Search for KPIs related to build performance"
  2. "Get active action items for my team"

### F. Engineering Support (ES) MCP server

- Access engineering systems knowledge base
- Diagnose issues with engineering assets
- Search ADO wikis, work items, and incident management
- **Demo action ideas:**
  1. "Search engineering documentation for deployment procedures"
  2. "Resolve this engineering system entity ID"

## Example Plan + Agent Interaction 


### A. Example Plan (in Plan Mode)
"Outline a refactoring plan for the UserService class in the LegacyApp project according to clean code principles. List the files you will modify and the specific changes you will make. Stop after presenting the plan for my approval."

### B. Example Execution (in Agent Mode)
"Proceed with step 1 of the approved refactoring plan for the UserService class in the LegacyApp project."


## Tie It Together: Reporting AI Usage in PRs and What We Do With It (4 min)

### A. What we report (show a real PR excerpt)

Your ADO PR notifications include a consistent "Code Contribution Analysis" block:

- Approximate contribution %
- Files with involvement
- Approximate AI-generated lines
- Model used (example: Claude Sonnet 4.5)
- Unit tests created count

Use this as your "standard PR footer" template:

```text
Copilot Usage Summary
- Approximate Copilot Contribution:
- Files with Copilot Involvement:
- Approximate Lines of Copilot-Generated Code:
- Model Used:
- Number of Unit Tests Created:
- Validation Performed:
```

### B. What we do with the info

#### a. Track adoption and engagement

- There is a stated goal to unify AI metrics across tools (GitHub Copilot, AI coding agents, M365) and integrate into reporting

#### b. Correlate usage to productivity outcomes

- CAP metrics describe identifying AI-involved PRs and comparing cycle time to non-AI PRs

#### c. Define what "engaged usage" means

- The "Engaged usage" definition explicitly counts deliberate interactions like prompting in Copilot Chat or Agent Mode
