# CrewAI

First steps following and modifying [this](https://realpython.com/crewai-python/) tutorial.

## 🔨 Project requirements

- Python managed by uv
- Ollama

## 🤷‍♂️ How do I run it?

So far you should have an [Ollama](https://docs.ollama.com/) server instance running (we will not enter into the details, check its documentation). Before everything, we need to pull the required model (tweak here and in `main.py` if needed).

```bash
# Configure model quantization before serving:
$env:OLLAMA_KV_CACHE_TYPE = 'Q4_K_M'
ollama serve

# For smaller GPUs peak this:
# ollama pull qwen2.5-coder:7b

ollama pull qwen3.6:35b
```

Now we can sync the project and run the model (there is a delay for loading the model in memory the first time - and if it times-out).

```bash
uv sync
uv run python main.py
```
