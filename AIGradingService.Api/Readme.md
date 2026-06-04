# AI Grading Service

## Опис проєкту

AI Grading Service — це прототип сервісу для пояснюваного AI-assisted оцінювання студентських програмних робіт мовою Python. Сервіс виконує студентський код на тест-кейсах, аналізує результати, порівнює різні baseline-и оцінювання та формує попередню оцінку з поясненнями.

Фінальне рішення не приймається автоматично: усі результати мають політику `teacher_must_confirm`.

## Основні можливості

- запуск Python-коду на тест-кейсах;
- підтримка weighted test cases;
- rule-based оцінювання;
- статичний аналіз коду;
- hybrid rule-based baseline;
- LLM-only оцінювання;
- test-aware LLM оцінювання;
- guarded LLM оцінювання;
- інтеграція з OpenRouter API;
- retry policy для LLM API;
- обробка некоректного JSON від LLM;
- розрахунок метрик якості.

## Технології

- .NET / ASP.NET Core Web API;
- C#;
- Python runtime;
- CSV / JSON / JSONL;
- OpenRouter API;
- Swagger / OpenAPI.

## Структура dataset

Dataset складається з таких файлів:

- `assignments.csv` — опис завдань;
- `submissions.jsonl` — програмні подання;
- `test_cases.csv` — тест-кейси та їхні ваги;
- `rubric.json` — критерії оцінювання.

## Baseline-и оцінювання

У системі реалізовано такі baseline-и:

| Baseline | Опис |
|---|---|
| `equal_weight_rule_based` | усі тести мають однакову вагу |
| `weighted_rule_based` | тести мають різні ваги |
| `static_analysis_rule_based` | оцінювання на основі структури коду |
| `hybrid_rule_based` | weighted tests + static penalties |
| `llm_only_grading` | LLM оцінює код без результатів тестів |
| `test_aware_llm_grading` | LLM оцінює код з урахуванням тестів |
| `test_aware_llm_guarded_grading` | test-aware LLM + deterministic guardrails |

## LLM-моделі

Для експериментів використовувались моделі через OpenRouter API:

- `nvidia/nemotron-3-super-120b-a12b:free`;
- `google/gemma-4-31b-it:free`;
- `liquid/lfm-2.5-1.2b-instruct:free`.

## Метрики

Для порівняння методів використовуються:

- MAE;
- Exact Match Accuracy;
- Within-One Accuracy;
- Max Absolute Error;
- Large Error Rate;
- Overestimation Rate;
- Underestimation Rate;
- Guardrail Applied Rate.

## Приклад запуску

```http
POST /api/pipeline/dataset/baselines
```

### Оцінювання одного подання конкретним baseline-ом:

```http
POST /api/pipeline/submissions/S_L2_T3_002/baselines/test_aware_llm_guarded_grading
```

## Налаштування OpenRouter

API-ключ задається через user secrets:

```bash
dotnet user-secrets init
dotnet user-secrets set "OpenRouter:ApiKey" "YOUR_API_KEY"
```

Приклад конфігурації:
```json
{
  "Llm": {
    "Provider": "OpenRouter"
  },
  "OpenRouter": {
    "Model": "nvidia/nemotron-3-super-120b-a12b:free",
    "BaseUrl": "https://openrouter.ai/api/v1/chat/completions",
    "AppTitle": "AIGradingService",
    "HttpReferer": "http://localhost"
  }
}
```
