import { useEffect, useMemo, useState } from 'react';
import {
  AppBar,
  Box,
  Button,
  Container,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Toolbar,
  Typography
} from '@mui/material';
import axios from 'axios';

export type Mention = {
  id: string;
  network: string;
  channelName: string;
  summary?: string;
  text?: string;
  priority: number;
  seen: boolean;
  createdAt: string;
  threadKey?: string;
  authorDisplayName?: string;
};

export default function MentionsPage() {
  const [mentions, setMentions] = useState<Mention[]>([]);
  const [priority, setPriority] = useState<number>(0);
  const [showSeen, setShowSeen] = useState<boolean | undefined>(undefined);

  const loadMentions = async () => {
    const params: Record<string, string> = {};
    if (priority > 0) {
      params.priority = priority.toString();
    }
    if (showSeen !== undefined) {
      params.seen = showSeen ? 'true' : 'false';
    }

    const response = await axios.get<Mention[]>('/api/mentions', { params });
    setMentions(response.data);
  };

  useEffect(() => {
    loadMentions();
  }, [priority, showSeen]);

  const grouped = useMemo(() => {
    return mentions.reduce<Record<string, Mention[]>>((acc, mention) => {
      const key = mention.network;
      acc[key] = acc[key] ?? [];
      acc[key].push(mention);
      return acc;
    }, {});
  }, [mentions]);

  return (
    <Box sx={{ flexGrow: 1 }}>
      <AppBar position="static" color="default" elevation={0}>
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1 }}>
            MentionSync Inbox
          </Typography>
          <FormControl size="small" sx={{ mr: 2, minWidth: 140 }}>
            <InputLabel id="priority-label">Min Priority</InputLabel>
            <Select
              labelId="priority-label"
              label="Min Priority"
              value={priority}
              onChange={(event) => setPriority(Number(event.target.value))}
            >
              <MenuItem value={0}>All</MenuItem>
              <MenuItem value={1}>P1+</MenuItem>
              <MenuItem value={2}>P2+</MenuItem>
              <MenuItem value={3}>P3</MenuItem>
            </Select>
          </FormControl>
          <FormControl size="small" sx={{ minWidth: 140 }}>
            <InputLabel id="seen-label">Status</InputLabel>
            <Select
              labelId="seen-label"
              label="Status"
              value={showSeen === undefined ? 'all' : showSeen ? 'seen' : 'unseen'}
              onChange={(event) => {
                const value = event.target.value;
                setShowSeen(value === 'all' ? undefined : value === 'seen');
              }}
            >
              <MenuItem value="all">All</MenuItem>
              <MenuItem value="unseen">Unseen</MenuItem>
              <MenuItem value="seen">Seen</MenuItem>
            </Select>
          </FormControl>
          <Button color="primary" variant="contained" sx={{ ml: 2 }} onClick={loadMentions}>
            Refresh
          </Button>
        </Toolbar>
      </AppBar>
      <Container maxWidth="lg" sx={{ py: 4 }}>
        {Object.entries(grouped).map(([network, items]) => (
          <Box key={network} sx={{ mb: 4 }}>
            <Typography variant="h6" gutterBottom>
              {network.toUpperCase()}
            </Typography>
            {items.map((mention) => (
              <Box
                key={mention.id}
                sx={{
                  p: 2,
                  mb: 2,
                  borderRadius: 2,
                  border: '1px solid',
                  borderColor: mention.seen ? 'grey.300' : 'primary.main',
                  backgroundColor: mention.seen ? 'background.paper' : 'primary.light',
                  color: mention.seen ? 'text.primary' : 'primary.contrastText'
                }}
              >
                <Typography variant="subtitle2">
                  {mention.channelName} • {new Date(mention.createdAt).toLocaleString()} • P{mention.priority}
                </Typography>
                <Typography variant="body1" sx={{ mt: 1 }}>
                  {mention.summary ?? mention.text}
                </Typography>
                {mention.threadKey && (
                  <Typography variant="caption" sx={{ display: 'block', mt: 1 }}>
                    Thread: {mention.threadKey}
                  </Typography>
                )}
                {mention.authorDisplayName && (
                  <Typography variant="caption" sx={{ display: 'block' }}>
                    From: {mention.authorDisplayName}
                  </Typography>
                )}
              </Box>
            ))}
          </Box>
        ))}
      </Container>
    </Box>
  );
}
