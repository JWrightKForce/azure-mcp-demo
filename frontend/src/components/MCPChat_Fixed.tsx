import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Grid,
  Paper,
  Chip,
  IconButton,
  AttachMoney,
  Security,
  CloudQueue,
  Backup,
  InsertDriveFile,
  Refresh,
  Send
} from '@mui/material';
import { MCPService } from '../services/MCPService';

interface ChatMessage {
  id: string;
  type: 'user' | 'assistant';
  content: string;
  timestamp: Date;
  formattedData?: any;
}

const MCPChat: React.FC = () => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputValue, setInputValue] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [consoleLogs, setConsoleLogs] = useState<string[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<'connected' | 'disconnected' | 'connecting'>('connected');

  // Add log to web console
  const addConsoleLog = (message: string) => {
    const timestamp = new Date().toLocaleTimeString();
    // Remove emojis from console logs for cleaner display
    const cleanMessage = message.replace(/[🚀📡⏰✅❌🔄➕📊🔍📝🎉📍]/g, '');
    setConsoleLogs(prev => [...prev, `[${timestamp}] ${cleanMessage}`]);
  };

  const sampleCommands = [
    'Show me my resource costs',
    'Check for security vulnerabilities',
    'Deploy a new web app to production',
    'Query Application Insights for errors',
    'Create a backup policy for my database',
  ];

  const formatResponse = (response: string, command: string) => {
    try {
      const data = JSON.parse(response);
      const lowerCommand = command.toLowerCase();

      // Log MCP interaction for client review
      console.log('🔍 MCP Response Analysis:');
      console.log('Command:', command);
      console.log('Response Type:', typeof data);
      console.log('Response Data:', data);

      if (lowerCommand.includes('cost')) {
        // Handle the new formatted MCP response
        if (data.result && data.result.Summary) {
          console.log('📊 Cost Data Detected:', data.result);
          return {
            type: 'cost',
            data: data.result
          };
        }
        // Handle old format for compatibility
        else if (data.TotalCost) {
          console.log('📊 Legacy Cost Data Detected:', data);
          return {
            type: 'cost',
            data: data
          };
        }
      }

      if (lowerCommand.includes('audit') || lowerCommand.includes('security')) {
        console.log('🔒 Security Data Detected:', data);
        return {
          type: 'security',
          data: data.result || data
        };
      }

      if (lowerCommand.includes('deploy')) {
        console.log('🚀 Deployment Data Detected:', data);
        return {
          type: 'deployment',
          data: data.result || data
        };
      }

      if (lowerCommand.includes('backup')) {
        console.log('💾 Backup Data Detected:', data);
        return {
          type: 'backup',
          data: data.result || data
        };
      }

      if (lowerCommand.includes('log') || lowerCommand.includes('query')) {
        console.log('📋 Log Data Detected:', data);
        return {
          type: 'logs',
          data: data.result || data
        };
      }

      console.log('📝 Text Response:', data);
      return { type: 'text', data: data.result || data };
    } catch (error) {
      console.log('❌ Parse Error:', error);
      return { type: 'text', data: response };
    }
  };

  const handleSendMessage = async () => {
    if (!inputValue.trim()) return;

    addConsoleLog(`Sending message: ${inputValue}`);
    addConsoleLog(`Current messages count: ${messages.length}`);

    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      type: 'user',
      content: inputValue,
      timestamp: new Date(),
    };

    setMessages(prev => {
      addConsoleLog(`Adding user message, new count: ${prev.length + 1}`);
      return [...prev, userMessage];
    });
    setInputValue('');
    setIsLoading(true);

    try {
      const response = await MCPService.executeTool(inputValue, addConsoleLog);
      const formatted = formatResponse(response, inputValue);
      
      const assistantMessage: ChatMessage = {
        id: (Date.now() + 1).toString(),
        type: 'assistant',
        content: response,
        timestamp: new Date(),
        formattedData: formatted,
      };

      setMessages(prev => {
        addConsoleLog(`Adding assistant message, new count: ${prev.length + 1}`);
        addConsoleLog(`Assistant message data: ${JSON.stringify(formatted)}`);
        return [...prev, assistantMessage];
      });
    } catch (error) {
      addConsoleLog(`Chat error: ${error instanceof Error ? error.message : 'Unknown error'}`);
      const errorMessage: ChatMessage = {
        id: (Date.now() + 1).toString(),
        type: 'assistant',
        content: `Error: ${error instanceof Error ? error.message : 'Unknown error occurred'}`,
        timestamp: new Date(),
      };
      setMessages(prev => [...prev, errorMessage]);
    } finally {
      addConsoleLog(`Resetting loading state`);
      setIsLoading(false);
      addConsoleLog(`Message send complete, total messages: ${messages.length + 2}`);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  const checkConnection = async () => {
    setConnectionStatus('connecting');
    try {
      const health = await MCPService.checkHealth();
      setConnectionStatus('connected');
    } catch (error) {
      setConnectionStatus('disconnected');
    }
  };

  const renderFormattedResponse = (formattedData: any) => {
    switch (formattedData.type) {
      case 'cost':
        return (
          <Box>
            <Grid container spacing={2} mb={2}>
              <Grid item xs={12} sm={6}>
                <Card variant="outlined">
                  <CardContent>
                    <Box display="flex" alignItems="center" mb={1}>
                      <AttachMoney color="primary" sx={{ mr: 1 }} />
                      <Typography variant="h6">Total Cost</Typography>
                    </Box>
                    <Typography variant="h4" color="primary">
                      {formattedData.data.Summary}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {formattedData.data.Period}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {formattedData.data.Resources ? formattedData.data.Resources.length : 0} resources
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
              <Grid item xs={12} sm={6}>
                <Card variant="outlined">
                  <CardContent>
                    <Typography variant="h6" mb={2}>Resources</Typography>
                    {formattedData.data.Resources && formattedData.data.Resources.map((resource: any, index: number) => (
                      <Box key={index} mb={1}>
                        <Typography variant="body2" fontWeight="bold">
                          {resource.ResourceName}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {resource.ResourceType}
                        </Typography>
                        <Typography variant="body1" color="primary">
                          ${resource.Cost.toFixed(2)} {resource.Currency}
                        </Typography>
                      </Box>
                    ))}
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </Box>
        );

      default:
        return (
          <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
            {formattedData.data}
          </Typography>
        );
    }
  };

  useEffect(() => {
    checkConnection();
  }, []);

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          MCP Chat Interface
        </Typography>
        <Box display="flex" alignItems="center" gap={2}>
          <Chip
            label={connectionStatus}
            color={connectionStatus === 'connected' ? 'success' : connectionStatus === 'connecting' ? 'warning' : 'error'}
            size="small"
          />
          <IconButton onClick={checkConnection} size="small">
            <Refresh />
          </IconButton>
        </Box>
      </Box>

      <Grid container spacing={2}>
        {/* Chat Area */}
        <Grid item xs={12} md={8}>
          <Card sx={{ height: '600px', display: 'flex', flexDirection: 'column' }}>
            <CardContent sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', p: 2 }}>
              {/* Messages Area */}
              <Box sx={{ flexGrow: 1, overflow: 'auto', mb: 2, maxHeight: '450px' }}>
                {messages.map((message) => (
                  <Box
                    key={message.id}
                    sx={{
                      mb: 2,
                      display: 'flex',
                      justifyContent: message.type === 'user' ? 'flex-end' : 'flex-start',
                    }}
                  >
                    <Paper
                      sx={{
                        p: 2,
                        maxWidth: message.type === 'user' ? '70%' : '90%',
                        bgcolor: message.type === 'user' ? 'primary.main' : 'grey.100',
                        color: message.type === 'user' ? 'white' : 'text.primary',
                      }}
                    >
                      {message.type === 'assistant' && message.formattedData ? (
                        renderFormattedResponse(message.formattedData)
                      ) : (
                        <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                          {message.content}
                        </Typography>
                      )}
                      <Typography variant="caption" sx={{ mt: 1, opacity: 0.7 }}>
                        {message.timestamp.toLocaleTimeString()}
                      </Typography>
                    </Paper>
                  </Box>
                ))}
              </Box>
              
              {/* Input Area */}
              <Box display="flex" gap={1} sx={{ borderTop: '1px solid #ddd', pt: 2 }}>
                <TextField
                  fullWidth
                  multiline
                  maxRows={3}
                  value={inputValue}
                  onChange={(e) => setInputValue(e.target.value)}
                  onKeyPress={handleKeyPress}
                  placeholder="Ask me about your Azure resources..."
                  disabled={isLoading}
                />
                <Button
                  variant="contained"
                  onClick={handleSendMessage}
                  disabled={!inputValue.trim() || isLoading}
                  sx={{ alignSelf: 'flex-end' }}
                >
                  <Send />
                </Button>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Console Area */}
        <Grid item xs={12} md={4}>
          <Card sx={{ height: '600px', display: 'flex', flexDirection: 'column' }}>
            <CardContent sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', p: 2 }}>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                <Typography variant="h6">
                  MCP Console
                </Typography>
                <Button 
                  variant="outlined"
                  size="small" 
                  onClick={() => setConsoleLogs([])}
                  sx={{ 
                    color: 'lime',
                    borderColor: 'lime',
                    '&:hover': {
                      borderColor: 'white',
                      color: 'white',
                      bgcolor: 'rgba(0,255,0,0.1)'
                    }
                  }}
                >
                  Clear Console
                </Button>
              </Box>
              <Box sx={{ 
                height: '480px',
                overflowY: 'scroll', 
                overflowX: 'hidden',
                bgcolor: 'black', 
                color: 'lime', 
                p: 1, 
                fontFamily: 'monospace', 
                fontSize: '0.8rem',
                borderRadius: 1,
                border: '1px solid #333',
                scrollbarWidth: 'thin',
                scrollbarColor: '#4CAF50 #1a1a1a',
                '&::-webkit-scrollbar': {
                  width: '8px',
                },
                '&::-webkit-scrollbar-track': {
                  background: '#1a1a1a',
                  borderRadius: '4px',
                },
                '&::-webkit-scrollbar-thumb': {
                  background: '#4CAF50',
                  borderRadius: '4px',
                  '&:hover': {
                    background: '#81C784',
                  }
                }
              }}>
                {consoleLogs.map((log, index) => (
                  <Box key={index} sx={{ mb: 0.5, wordBreak: 'break-all' }}>
                    {log}
                  </Box>
                ))}
                {consoleLogs.length === 0 && (
                  <Typography sx={{ color: 'gray', fontStyle: 'italic' }}>
                    Console logs will appear here...
                  </Typography>
                )}
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default MCPChat;
