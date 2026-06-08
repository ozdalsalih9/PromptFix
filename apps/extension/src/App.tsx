import { Check, Clipboard, Loader2, Trash2, WandSparkles } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'

type Language = 'auto' | 'tr' | 'en'
type Mode = 'general' | 'coding' | 'career' | 'academic' | 'image' | 'email'
type Style = 'balanced' | 'concise' | 'detailed' | 'professional'

type ImprovePromptResponse = {
  improvedPrompt: string
  shortVersion: string
  whyBetter: string[]
  missingContext: string[]
  model: string
}

type HistoryItem = {
  id: string
  createdAt: string
  prompt: string
  language: Language
  mode: Mode
  style: Style
  response: ImprovePromptResponse
}

const historyKey = 'promptforge.history'
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5064'

const languages: Array<{ value: Language; label: string }> = [
  { value: 'auto', label: 'Auto' },
  { value: 'tr', label: 'Turkish' },
  { value: 'en', label: 'English' },
]

const modes: Array<{ value: Mode; label: string }> = [
  { value: 'general', label: 'General' },
  { value: 'coding', label: 'Coding' },
  { value: 'career', label: 'Career' },
  { value: 'academic', label: 'Academic' },
  { value: 'image', label: 'Image' },
  { value: 'email', label: 'Email' },
]

const styles: Array<{ value: Style; label: string }> = [
  { value: 'balanced', label: 'Balanced' },
  { value: 'concise', label: 'Concise' },
  { value: 'detailed', label: 'Detailed' },
  { value: 'professional', label: 'Professional' },
]

function getChromeStorage() {
  return globalThis.chrome?.storage?.local
}

async function readHistory(): Promise<HistoryItem[]> {
  const chromeStorage = getChromeStorage()

  if (chromeStorage) {
    const result = await chromeStorage.get(historyKey)
    return Array.isArray(result[historyKey]) ? result[historyKey] : []
  }

  const value = localStorage.getItem(historyKey)
  return value ? JSON.parse(value) : []
}

async function writeHistory(items: HistoryItem[]) {
  const chromeStorage = getChromeStorage()

  if (chromeStorage) {
    await chromeStorage.set({ [historyKey]: items })
    return
  }

  localStorage.setItem(historyKey, JSON.stringify(items))
}

async function improvePrompt(input: {
  prompt: string
  language: Language
  mode: Mode
  style: Style
}): Promise<ImprovePromptResponse> {
  const response = await fetch(`${apiBaseUrl}/api/prompt/improve`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(input),
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(parseApiError(text) || `Request failed with status ${response.status}`)
  }

  return response.json()
}

function parseApiError(text: string) {
  if (!text) {
    return ''
  }

  try {
    const parsed = JSON.parse(text) as { message?: string; title?: string; errors?: Record<string, string[]> }

    if (parsed.message) {
      return parsed.message
    }

    if (parsed.errors) {
      return Object.values(parsed.errors).flat().join(' ')
    }

    return parsed.title ?? text
  } catch {
    return text
  }
}

function App() {
  const [prompt, setPrompt] = useState('')
  const [language, setLanguage] = useState<Language>('auto')
  const [mode, setMode] = useState<Mode>('general')
  const [style, setStyle] = useState<Style>('balanced')
  const [result, setResult] = useState<ImprovePromptResponse | null>(null)
  const [history, setHistory] = useState<HistoryItem[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [copiedKey, setCopiedKey] = useState<string | null>(null)

  const canSubmit = useMemo(() => prompt.trim().length >= 3 && !isLoading, [isLoading, prompt])

  useEffect(() => {
    readHistory()
      .then(setHistory)
      .catch(() => setHistory([]))
  }, [])

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!canSubmit) {
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      const response = await improvePrompt({
        prompt: prompt.trim(),
        language,
        mode,
        style,
      })

      setResult(response)

      const nextHistory = [
        {
          id: crypto.randomUUID(),
          createdAt: new Date().toISOString(),
          prompt: prompt.trim(),
          language,
          mode,
          style,
          response,
        },
        ...history,
      ].slice(0, 20)

      setHistory(nextHistory)
      await writeHistory(nextHistory)
    } catch (caught) {
      const message = caught instanceof Error ? caught.message : 'Prompt could not be improved.'
      setError(message)
    } finally {
      setIsLoading(false)
    }
  }

  async function copyText(key: string, text: string) {
    await navigator.clipboard.writeText(text)
    setCopiedKey(key)
    window.setTimeout(() => setCopiedKey(null), 1200)
  }

  async function clearHistory() {
    setHistory([])
    await writeHistory([])
  }

  function loadHistoryItem(item: HistoryItem) {
    setPrompt(item.prompt)
    setLanguage(item.language)
    setMode(item.mode)
    setStyle(item.style)
    setResult(item.response)
    setError(null)
  }

  return (
    <main className="popup-shell">
      <header className="app-header">
        <div>
          <p className="eyebrow">PromptForge</p>
          <h1>Improve a prompt</h1>
        </div>
        <span className="status-pill">MVP</span>
      </header>

      <form className="prompt-form" onSubmit={onSubmit}>
        <label className="field-block">
          <span>Prompt</span>
          <textarea
            value={prompt}
            onChange={(event) => setPrompt(event.target.value)}
            placeholder="Paste a rough prompt..."
            maxLength={4000}
          />
        </label>

        <div className="control-grid">
          <label>
            <span>Language</span>
            <select value={language} onChange={(event) => setLanguage(event.target.value as Language)}>
              {languages.map((item) => (
                <option key={item.value} value={item.value}>
                  {item.label}
                </option>
              ))}
            </select>
          </label>

          <label>
            <span>Mode</span>
            <select value={mode} onChange={(event) => setMode(event.target.value as Mode)}>
              {modes.map((item) => (
                <option key={item.value} value={item.value}>
                  {item.label}
                </option>
              ))}
            </select>
          </label>

          <label>
            <span>Style</span>
            <select value={style} onChange={(event) => setStyle(event.target.value as Style)}>
              {styles.map((item) => (
                <option key={item.value} value={item.value}>
                  {item.label}
                </option>
              ))}
            </select>
          </label>
        </div>

        <button className="primary-button" type="submit" disabled={!canSubmit}>
          {isLoading ? <Loader2 className="spin" size={18} /> : <WandSparkles size={18} />}
          Improve Prompt
        </button>
      </form>

      {error ? <div className="error-box">{error}</div> : null}

      {result ? (
        <section className="result-stack" aria-live="polite">
          <OutputBlock
            title="Improved prompt"
            copyKey="improved"
            copiedKey={copiedKey}
            onCopy={copyText}
            text={result.improvedPrompt}
            primary
          />
          <div className="reason-box">
            <div className="section-heading">Why better</div>
            <ul>
              {result.whyBetter.map((reason) => (
                <li key={reason}>{reason}</li>
              ))}
            </ul>
          </div>
          <div className="reason-box">
            <div className="section-heading">Missing context</div>
            {result.missingContext.length === 0 ? (
              <p>No critical missing context detected.</p>
            ) : (
              <ul>
                {result.missingContext.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            )}
          </div>
          <OutputBlock
            title="Short version"
            copyKey="short"
            copiedKey={copiedKey}
            onCopy={copyText}
            text={result.shortVersion}
          />
          <p className="model-note">Model: {result.model}</p>
        </section>
      ) : null}

      <section className="history-section">
        <div className="history-header">
          <div className="section-heading">History</div>
          <button className="icon-button" type="button" onClick={clearHistory} disabled={history.length === 0} title="Clear history">
            <Trash2 size={16} />
          </button>
        </div>

        {history.length === 0 ? (
          <p className="empty-state">No saved prompts yet.</p>
        ) : (
          <div className="history-list">
            {history.map((item) => (
              <button key={item.id} className="history-item" type="button" onClick={() => loadHistoryItem(item)}>
                <span>{item.prompt}</span>
                <small>{new Date(item.createdAt).toLocaleString()}</small>
              </button>
            ))}
          </div>
        )}
      </section>
    </main>
  )
}

type OutputBlockProps = {
  title: string
  text: string
  copyKey: string
  copiedKey: string | null
  primary?: boolean
  onCopy: (key: string, text: string) => Promise<void>
}

function OutputBlock({ title, text, copyKey, copiedKey, primary = false, onCopy }: OutputBlockProps) {
  const copied = copiedKey === copyKey

  return (
    <article className={primary ? 'output-block output-block-primary' : 'output-block'}>
      <div className="output-header">
        <div className="section-heading">{title}</div>
        <button className="icon-button" type="button" onClick={() => onCopy(copyKey, text)} title={`Copy ${title}`}>
          {copied ? <Check size={16} /> : <Clipboard size={16} />}
        </button>
      </div>
      <p>{text}</p>
    </article>
  )
}

export default App
