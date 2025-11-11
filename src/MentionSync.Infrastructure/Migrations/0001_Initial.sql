CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE tenant (
  id uuid PRIMARY KEY,
  name text NOT NULL,
  created_at timestamptz DEFAULT now()
);

CREATE TABLE app_user (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenant(id),
  email text NOT NULL,
  display_name text,
  created_at timestamptz DEFAULT now(),
  UNIQUE (tenant_id, email)
);

CREATE TABLE identity (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenant(id),
  app_user_id uuid REFERENCES app_user(id),
  network text NOT NULL CHECK (network IN ('slack','teams','gchat','discord')),
  external_user_id text NOT NULL,
  handle text,
  email text,
  UNIQUE (tenant_id, network, external_user_id)
);

CREATE TABLE integration (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenant(id),
  network text NOT NULL,
  config jsonb NOT NULL,
  created_at timestamptz DEFAULT now()
);

CREATE TABLE channel (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenant(id),
  network text NOT NULL,
  external_channel_id text NOT NULL,
  name text,
  UNIQUE (tenant_id, network, external_channel_id)
);

CREATE TABLE source_message (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenant(id),
  network text NOT NULL,
  external_message_id text NOT NULL,
  channel_id uuid REFERENCES channel(id),
  author_identity_id uuid REFERENCES identity(id),
  ts timestamptz NOT NULL,
  text text,
  raw jsonb NOT NULL,
  thread_key text,
  UNIQUE (tenant_id, network, external_message_id)
);

CREATE TABLE mention (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenant(id),
  source_message_id uuid NOT NULL REFERENCES source_message(id) ON DELETE CASCADE,
  mentioned_identity_id uuid REFERENCES identity(id),
  matched_rule text NOT NULL,
  is_explicit boolean NOT NULL,
  confidence real NOT NULL,
  created_at timestamptz DEFAULT now(),
  seen boolean DEFAULT false,
  priority int DEFAULT 0,
  summary text
);

CREATE INDEX mention_seen_priority_idx ON mention (tenant_id, seen, priority, created_at DESC);
