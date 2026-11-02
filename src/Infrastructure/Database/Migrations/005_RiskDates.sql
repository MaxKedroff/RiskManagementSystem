-- Migration 005: when a risk was registered and closed (registry period filter, dashboard, archive)

ALTER TABLE risk.risks
    ADD COLUMN IF NOT EXISTS risk_created_at timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS risk_closed_at timestamptz NULL;

-- Existing risks: the first assessment is the closest known date
UPDATE risk.risks r
SET risk_created_at = a.first_at
FROM (SELECT risk_id, min(created_at) AS first_at FROM assessment.assessments GROUP BY risk_id) a
WHERE a.risk_id = r.risk_id AND a.first_at < r.risk_created_at;
