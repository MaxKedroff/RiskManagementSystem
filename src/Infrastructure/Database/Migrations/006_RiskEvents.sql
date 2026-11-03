-- Migration 006: risk history — what happened to a risk and its measures, by whom and when.
-- Feeds the «История» tab of the risk card and the «Решения по рискам» report.

CREATE TABLE IF NOT EXISTS risk.risk_events (
    event_id        integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    risk_id         integer NOT NULL REFERENCES risk.risks(risk_id),
    -- create | assess | mode | measure | approval | done | system
    event_kind      varchar(20) NOT NULL
        CHECK (event_kind IN ('create', 'assess', 'mode', 'measure', 'approval', 'done', 'system')),
    event_text      text NOT NULL,
    -- NULL for system events (cycle completion)
    event_author_id integer NULL REFERENCES roles.users(user_id),
    event_created   timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_risk_events_risk ON risk.risk_events (risk_id, event_created DESC);
