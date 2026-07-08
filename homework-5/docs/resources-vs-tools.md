# Resources vs. Tools in MCP

The Model Context Protocol (MCP) exposes two distinct kinds of capabilities to an AI client like
Claude. This homework's custom server implements one of each.

## Resource

**Resources are URIs that Claude can read from** — files, API endpoints, database rows, or any
addressable content. They are *passive*: reading a resource returns data and does not change state.
Claude (or the user) chooses to read a resource by its URI, much like opening a file path or fetching a
URL.

In this server, the Resource is the lorem-ipsum source, addressable two ways:

| URI | Returns |
|---|---|
| `lorem://words` | the default 30 words of `lorem-ipsum.md` |
| `lorem://words/{word_count}` | exactly `word_count` words (e.g. `lorem://words/7` → 7 words) |

Reading `lorem://words` is a read-only lookup: it hands back text and has no side effects.

## Tool

**Tools are actions Claude can call to perform operations** — reading a file, running a command,
querying a service, sending a message. They are *active*: Claude invokes a tool by name with arguments,
and the tool runs code to produce a result (and may have side effects).

In this server, the Tool is:

| Tool | Signature | Does |
|---|---|---|
| `read` | `read(word_count: int = 30)` | returns the first `word_count` words of the lorem-ipsum source |

Claude calls `read` (optionally passing `word_count`) and the server executes the word-limiting logic to
build the response.

## How they relate here

Both the `read` Tool and the `lorem://words` Resources delegate to the same internal `_read_words`
helper. The difference is *how Claude reaches the content*:

- **Resource** — Claude **reads** a URI (`lorem://words`). Best when the content is naturally addressable
  and the client wants to pull it as context.
- **Tool** — Claude **calls** an action (`read`) with parameters. Best when the client wants to invoke
  behavior and pass arguments explicitly.

Exposing both is intentional: it demonstrates the two MCP surfaces over a single underlying behavior.
