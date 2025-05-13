/*
  # Device Status Logs Collection

  1. New Tables
    - `device_status_logs`
      - `id` (uuid, primary key)
      - `device_id` (text, references devices)
      - `status` (text) - For online/offline status
      - `access_status` (text) - For open/closed status
      - `created_at` (timestamp)
      - `ip_address` (text)
      - `location` (text)
      - `group` (text)

  2. Security
    - Enable RLS on `device_status_logs` table
    - Add policy for authenticated users to read logs
    - Add policy for service role to insert logs
*/

CREATE TABLE IF NOT EXISTS device_status_logs (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  device_id text NOT NULL,
  status text,
  access_status text,
  created_at timestamptz DEFAULT now(),
  ip_address text NOT NULL,
  location text,
  "group" text
);

-- Enable RLS
ALTER TABLE device_status_logs ENABLE ROW LEVEL SECURITY;

-- Policy for reading logs
CREATE POLICY "Allow authenticated users to read logs"
  ON device_status_logs
  FOR SELECT
  TO authenticated
  USING (true);

-- Policy for inserting logs
CREATE POLICY "Allow service role to insert logs"
  ON device_status_logs
  FOR INSERT
  TO service_role
  WITH CHECK (true);