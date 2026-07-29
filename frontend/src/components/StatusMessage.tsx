interface StatusMessageProps {
  kind: 'loading' | 'error' | 'success'
  message: string
}

export default function StatusMessage({ kind, message }: StatusMessageProps) {
  return <div className={`status-message status-message--${kind}`}>{message}</div>
}
