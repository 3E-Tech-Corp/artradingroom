const INDICATORS = ['RSI', 'SMA_FAST', 'SMA_MEDIUM', 'SMA_SLOW', 'EMA_FAST', 'EMA_SLOW', 'BOLLINGER_UPPER', 'BOLLINGER_LOWER', 'MACD', 'PRICE']
const OPERATORS = ['<', '>', '<=', '>=', '==']
const SIZING_METHODS = ['fixed', 'proportional']

export const defaultRule = {
  entryConditions: [{ indicator: 'RSI', operator: '<', value: 30 }],
  exitConditions: [{ indicator: 'RSI', operator: '>', value: 70 }],
  positionSizing: { percentageOfPortfolio: 0.05, maxPositionSize: 0.20, sizingMethod: 'fixed' },
  parameters: { rsiPeriod: 14, smaFastPeriod: 20, smaMediumPeriod: 50, smaSlowPeriod: 200, bollingerPeriod: 20, bollingerStdDev: 2, emaFastPeriod: 12, emaSlowPeriod: 26 }
}

export default function ParameterEditor({ value = defaultRule, onChange }) {
  const data = value

  const update = (newData) => onChange(newData)

  const updateCond = (type, i, field, val) => {
    const conds = data[type].map((c, idx) =>
      idx === i ? { ...c, [field]: field === 'value' ? Number(val) : val } : c
    )
    update({ ...data, [type]: conds })
  }

  const addCond = (type) =>
    update({ ...data, [type]: [...data[type], { indicator: 'RSI', operator: '<', value: 30 }] })

  const removeCond = (type, i) =>
    update({ ...data, [type]: data[type].filter((_, idx) => idx !== i) })

  const setSizing = (field, val) =>
    update({ ...data, positionSizing: { ...data.positionSizing, [field]: field === 'sizingMethod' ? val : Number(val) } })

  const setParam = (field, val) =>
    update({ ...data, parameters: { ...data.parameters, [field]: Number(val) } })

  const sectionLabel = { fontSize: '0.72rem', color: '#8b949e', textTransform: 'uppercase', letterSpacing: '0.05em', display: 'block', marginBottom: '0.4rem' }
  const addBtnStyle = { background: '#1f6feb22', border: '1px solid #30363d', color: '#58a6ff', borderRadius: '4px', padding: '2px 8px', cursor: 'pointer', fontSize: '0.72rem', fontFamily: 'inherit' }
  const removeBtnStyle = { background: 'none', border: 'none', color: '#f85149', cursor: 'pointer', fontSize: '1rem', padding: '0 4px', lineHeight: 1 }
  const divider = { borderTop: '1px solid #21262d', margin: '0.75rem 0' }

  const renderConditions = (type, label) => (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.4rem' }}>
        <span style={sectionLabel}>{label}</span>
        <button type="button" style={addBtnStyle} onClick={() => addCond(type)}>+ Add</button>
      </div>
      {data[type].length === 0 && (
        <p style={{ fontSize: '0.75rem', color: '#484f58', fontStyle: 'italic', marginBottom: '0.5rem' }}>No conditions — add at least one</p>
      )}
      {data[type].map((c, i) => (
        <div key={i} style={{ display: 'flex', gap: '0.4rem', marginBottom: '0.35rem', alignItems: 'center' }}>
          <select value={c.indicator} onChange={e => updateCond(type, i, 'indicator', e.target.value)} style={{ flex: 2 }}>
            {INDICATORS.map(ind => <option key={ind}>{ind}</option>)}
          </select>
          <select value={c.operator} onChange={e => updateCond(type, i, 'operator', e.target.value)} style={{ flex: 1, minWidth: '54px' }}>
            {OPERATORS.map(op => <option key={op}>{op}</option>)}
          </select>
          <input type="number" value={c.value} step="0.1" onChange={e => updateCond(type, i, 'value', e.target.value)} style={{ flex: 1, minWidth: '64px' }} />
          <button type="button" style={removeBtnStyle} onClick={() => removeCond(type, i)}>×</button>
        </div>
      ))}
    </div>
  )

  return (
    <div>
      {renderConditions('entryConditions', 'Entry Conditions')}
      <div style={divider} />
      {renderConditions('exitConditions', 'Exit Conditions')}
      <div style={divider} />

      <span style={sectionLabel}>Indicator Periods</span>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '0.5rem', marginBottom: '0.75rem' }}>
        {[
          ['rsiPeriod', 'RSI Period', 1],
          ['smaFastPeriod', 'SMA Fast', 1],
          ['smaMediumPeriod', 'SMA Mid', 1],
          ['smaSlowPeriod', 'SMA Slow', 1],
          ['bollingerPeriod', 'BB Period', 1],
          ['bollingerStdDev', 'BB StdDev', 0.1],
          ['emaFastPeriod', 'EMA Fast', 1],
          ['emaSlowPeriod', 'EMA Slow', 1],
        ].map(([field, lbl, step]) => (
          <div key={field}>
            <label style={{ fontSize: '0.65rem', color: '#8b949e', display: 'block', marginBottom: '2px' }}>{lbl}</label>
            <input type="number" value={data.parameters[field] ?? ''} step={step} min="1" onChange={e => setParam(field, e.target.value)} />
          </div>
        ))}
      </div>

      <span style={sectionLabel}>Position Sizing</span>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '0.5rem' }}>
        <div>
          <label style={{ fontSize: '0.65rem', color: '#8b949e', display: 'block', marginBottom: '2px' }}>% of Portfolio</label>
          <input type="number" value={data.positionSizing.percentageOfPortfolio} step="0.01" min="0.01" max="1" onChange={e => setSizing('percentageOfPortfolio', e.target.value)} />
        </div>
        <div>
          <label style={{ fontSize: '0.65rem', color: '#8b949e', display: 'block', marginBottom: '2px' }}>Max Size</label>
          <input type="number" value={data.positionSizing.maxPositionSize} step="0.01" min="0.01" max="1" onChange={e => setSizing('maxPositionSize', e.target.value)} />
        </div>
        <div>
          <label style={{ fontSize: '0.65rem', color: '#8b949e', display: 'block', marginBottom: '2px' }}>Method</label>
          <select value={data.positionSizing.sizingMethod} onChange={e => setSizing('sizingMethod', e.target.value)}>
            {SIZING_METHODS.map(m => <option key={m}>{m}</option>)}
          </select>
        </div>
      </div>
    </div>
  )
}
