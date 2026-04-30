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
} from '@mui/material';
import MCPService from '../services/MCPService';

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

  const sampleCommands = [
    'Show me my resource costs',
    'Check for security vulnerabilities',
    'Deploy a new web app to production',
    'Query Application Insights for errors',
    'Create a backup policy for my database',
  ];

  // Add log to web console
  const addConsoleLog = (message: string) => {
    const timestamp = new Date().toLocaleTimeString();
    // Remove emojis from console logs for cleaner display
    const cleanMessage = message.replace(/[🚀📡⏰✅❌🔄➕📊🔍📝🎉📍]/g, '');
    setConsoleLogs(prev => [...prev, `[${timestamp}] ${cleanMessage}`]);
  };

  const formatResponse = (response: string, command: string) => {
    try {
      const data = JSON.parse(response);
      const lowerCommand = command.toLowerCase();

      // Check if response contains cost data
      if (data.result && data.result.Summary && data.result.Summary.includes('Total Cost')) {
                return {
          type: 'cost',
          data: data.result
        };
      }

      if (lowerCommand.includes('audit') || lowerCommand.includes('security')) {
                return {
          type: 'security',
          data: data.result || data
        };
      }

      // Check if response contains resource utilization data
      if (data.result && (Array.isArray(data.result) || data.result.TotalResources !== undefined || data.result.Resources)) {
                // If result is an array, wrap it in the expected format
        const resourceData = Array.isArray(data.result) 
          ? { Resources: data.result, TotalResources: data.result.length }
          : data.result || data;
        return {
          type: 'resources',
          data: resourceData,
          command: command // Store the command for filtering
        };
      }

      if (lowerCommand.includes('backup')) {
                return {
          type: 'backup',
          data: data.result || data
        };
      }

      if (lowerCommand.includes('log') || lowerCommand.includes('query')) {
                return {
          type: 'logs',
          data: data.result || data
        };
      }

      return { type: 'text', data: data.result || data };
    } catch (error) {
      return { type: 'text', data: response };
    }
  };

  const handleSendMessage = async () => {
    if (!inputValue.trim()) return;

    // Clear console for new question
    setConsoleLogs([]);
    addConsoleLog(`Starting new question: ${inputValue}`);
    addConsoleLog(`Current messages count: ${messages.length}`);

    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      type: 'user',
      content: inputValue,
      timestamp: new Date(),
    };

    setMessages(prev => {
      const newMessages = [...prev, userMessage];
      return newMessages;
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
            <Grid container spacing={2}>
              {/* Monthly Breakdown */}
              <Grid item xs={12} md={6}>
                <Card variant="outlined">
                  <CardContent>
                    <Box display="flex" alignItems="center" mb={2}>
                      <Typography variant="h6" color="primary" sx={{ mr: 1 }}>�</Typography>
                      <Typography variant="h6">Monthly Trend</Typography>
                    </Box>
                    {formattedData.data.MonthlyBreakdown && formattedData.data.MonthlyBreakdown.length > 0 ? (
                      formattedData.data.MonthlyBreakdown.map((month: any, index: number) => (
                        <Box key={index} mb={1} p={1} sx={{ border: 1, borderColor: 'divider', borderRadius: 1 }}>
                          <Typography variant="body2" fontWeight="bold">
                            {month.Month}
                          </Typography>
                          <Typography variant="h6" color="primary">
                            ${month.Cost.toFixed(2)}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {month.ResourceCount} resources
                          </Typography>
                        </Box>
                      ))
                    ) : (
                      <Typography variant="body2" color="text.secondary">
                        Monthly data not available
                      </Typography>
                    )}
                  </CardContent>
                </Card>
              </Grid>

              {/* Resource Breakdown */}
              <Grid item xs={12} md={6}>
                <Card variant="outlined">
                  <CardContent>
                    <Box display="flex" alignItems="center" mb={2}>
                      <Typography variant="h6" color="primary" sx={{ mr: 1 }}>💰</Typography>
                      <Typography variant="h6">Cost Analysis</Typography>
                    </Box>
                    <Typography variant="h4" color="primary" mb={1}>
                      {formattedData.data.Summary}
                    </Typography>
                    {formattedData.data.Resources && (
                      <Typography variant="caption" color="text.secondary" mb={2}>
                        📊 Total from {formattedData.data.Resources.length} resources
                      </Typography>
                    )}
                    {formattedData.data.Period && (
                      <Typography variant="body2" color="text.secondary" mb={2}>
                        {formattedData.data.Period}
                      </Typography>
                    )}
                    <Typography variant="h6" mb={2}>
                      Top {Math.min(10, formattedData.data.Resources?.length || 0)} Cost Contributors
                    </Typography>
                    <Typography variant="caption" color="text.secondary" mb={2}>
                      Showing {Math.min(10, formattedData.data.Resources?.length || 0)} of {formattedData.data.Resources?.length || 0} total resources
                    </Typography>
                    {formattedData.data.Resources && formattedData.data.Resources.length > 0 ? (
                      formattedData.data.Resources.slice(0, 10).map((resource: any, index: number) => (
                        <Box key={index} mb={1} p={1} sx={{ border: 1, borderColor: 'divider', borderRadius: 1 }}>
                          <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
                            <Typography variant="body2" fontWeight="bold">
                              {resource.ResourceName}
                            </Typography>
                            <Typography variant="h6" color="primary">
                              ${resource.Cost.toFixed(2)}
                            </Typography>
                          </Box>
                          <Typography variant="caption" color="text.secondary">
                            {resource.ResourceType}
                          </Typography>
                          <Typography variant="caption" color="info.main">
                            📊 {((resource.Cost / (formattedData.data.Resources?.reduce((sum: number, r: any) => sum + r.Cost, 0) || 1)) * 100).toFixed(1)}% of total cost
                          </Typography>
                        </Box>
                      ))
                    ) : (
                      <Typography variant="body2" color="text.secondary">
                        Resource breakdown not available
                      </Typography>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </Box>
        );

      case 'security':
        return (
          <Box>
            <Grid container spacing={2} mb={2}>
              <Grid item xs={12} sm={6}>
                <Card variant="outlined">
                  <CardContent>
                    <Box display="flex" alignItems="center" mb={1}>
                      <Typography variant="h6" sx={{ mr: 1 }}>🔒</Typography>
                      <Typography variant="h6">Security Audit</Typography>
                    </Box>
                    <Typography variant="body2" color="text.secondary">
                      Security vulnerability assessment completed
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Total resources scanned: {formattedData.data.TotalResources || 0}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Secure resources: {formattedData.data.SecureResources || 0}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Security score: {formattedData.data.SecurityScore || 0}/100
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
              <Grid item xs={12} sm={6}>
                <Card variant="outlined">
                  <CardContent>
                    <Typography variant="h6" mb={2}>Security Issues</Typography>
                    {formattedData.data.Issues && formattedData.data.Issues.length > 0 && (
                      <Box>
                        <Typography variant="body2" color="text.secondary" mb={1}>
                          {formattedData.data.Issues.length} security issues found
                        </Typography>
                        {formattedData.data.Issues.map((issue: any, index: number) => (
                          <Box key={index} mb={2} p={1} sx={{ border: 1, borderColor: 'divider', borderRadius: 1 }}>
                            <Typography variant="body2" fontWeight="bold" color={issue.Severity === 'High' ? 'error.main' : issue.Severity === 'Medium' ? 'warning.main' : 'info.main'}>
                              {issue.Severity}: {issue.IssueType}
                            </Typography>
                            <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5 }}>
                              Resource: {issue.ResourceType}
                            </Typography>
                            <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5 }}>
                              {issue.Description}
                            </Typography>
                            <Typography variant="caption" color="primary.main" sx={{ mt: 0.5 }}>
                              💡 {issue.Recommendation}
                            </Typography>
                          </Box>
                        ))}
                      </Box>
                    )}
                    {(!formattedData.data.Issues || formattedData.data.Issues.length === 0) && (
                      <Typography variant="body2" color="success.main">
                        ✅ No security issues detected
                      </Typography>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </Box>
        );

      case 'resources':
        return (
          <Box>
            <Card variant="outlined">
              <CardContent>
                <Box display="flex" alignItems="center" mb={2}>
                  <Typography variant="h6" sx={{ mr: 1 }}>📊</Typography>
                  <Typography variant="h6">Azure Resources</Typography>
                </Box>
                <Typography variant="body2" color="text.secondary" mb={2}>
                  Total resources found: {formattedData.data.TotalResources || 0}
                </Typography>
                {formattedData.data.Resources && (
                  <>
                    <Typography variant="body2" color="text.secondary" mb={1}>
                      Showing {formattedData.data.Resources.filter((resource: any) => {
                        const query = formattedData.command?.toLowerCase() || '';
                        const type = resource.Type.toLowerCase();
                        
                        // Database filtering - more comprehensive
                        if (query.includes('database') || query.includes('sql')) {
                          return type.includes('sql') || type.includes('database') || type.includes('microsoft.sql');
                        }
                        
                        // Web app filtering
                        if (query.includes('web') || query.includes('app')) {
                          return type.includes('web') || type.includes('app');
                        }
                        
                        // Storage filtering
                        if (query.includes('storage')) {
                          return type.includes('storage');
                        }
                        
                        // Key Vault filtering
                        if (query.includes('key') || query.includes('vault')) {
                          return type.includes('keyvault') || type.includes('key');
                        }
                        
                        // VM filtering
                        if (query.includes('vm') || query.includes('virtual') || query.includes('machine')) {
                          return type.includes('virtual') || type.includes('vm') || type.includes('compute');
                        }
                        
                        // Network filtering
                        if (query.includes('network') || query.includes('vnet')) {
                          return type.includes('network') || type.includes('vnet');
                        }
                        
                        return true; // Show all if no specific filter
                      }).length} matching resources
                    </Typography>
                    <Grid container spacing={2}>
                      {formattedData.data.Resources
                        .filter((resource: any) => {
                          const query = formattedData.command?.toLowerCase() || '';
                          const type = resource.Type.toLowerCase();
                          
                          // Database filtering - more comprehensive
                          if (query.includes('database') || query.includes('sql')) {
                            return type.includes('sql') || type.includes('database') || type.includes('microsoft.sql');
                          }
                          
                          // Web app filtering
                          if (query.includes('web') || query.includes('app')) {
                            return type.includes('web') || type.includes('app');
                          }
                          
                          // Storage filtering
                          if (query.includes('storage')) {
                            return type.includes('storage');
                          }
                          
                          // Key Vault filtering
                          if (query.includes('key') || query.includes('vault')) {
                            return type.includes('keyvault') || type.includes('key');
                          }
                          
                          // VM filtering
                          if (query.includes('vm') || query.includes('virtual') || query.includes('machine')) {
                            return type.includes('virtual') || type.includes('vm') || type.includes('compute');
                          }
                          
                          // Network filtering
                          if (query.includes('network') || query.includes('vnet')) {
                            return type.includes('network') || type.includes('vnet');
                          }
                          
                          return true; // Show all if no specific filter
                        })
                        .map((resource: any, index: number) => (
                          <Grid item xs={12} sm={6} key={index}>
                            <Card variant="outlined" sx={{ p: 1 }}>
                              <Typography variant="body2" fontWeight="bold">
                                {resource.Name}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {resource.Type}
                              </Typography>
                              <Typography variant="caption" color="text.secondary" display="block">
                                {resource.Location} • {resource.ResourceGroup}
                              </Typography>
                            </Card>
                          </Grid>
                        ))}
                    </Grid>
                  </>
                )}
              </CardContent>
            </Card>
          </Box>
        );

      case 'deployment':
        return (
          <Box>
            <Card variant="outlined">
              <CardContent>
                <Box display="flex" alignItems="center" mb={2}>
                  <Typography variant="h6" sx={{ mr: 1 }}>🚀</Typography>
                  <Typography variant="h6">Deployment Status</Typography>
                </Box>
                <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                  {JSON.stringify(formattedData.data, null, 2)}
                </Typography>
              </CardContent>
            </Card>
          </Box>
        );

      case 'backup':
        return (
          <Box>
            <Card variant="outlined">
              <CardContent>
                <Box display="flex" alignItems="center" mb={2}>
                  <Typography variant="h6" sx={{ mr: 1 }}>💾</Typography>
                  <Typography variant="h6">Backup Results</Typography>
                </Box>
                <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                  {JSON.stringify(formattedData.data, null, 2)}
                </Typography>
              </CardContent>
            </Card>
          </Box>
        );

      case 'logs':
        return (
          <Box>
            <Card variant="outlined">
              <CardContent>
                <Box display="flex" alignItems="center" mb={2}>
                  <Typography variant="h6" sx={{ mr: 1 }}>📋</Typography>
                  <Typography variant="h6">Log Analysis</Typography>
                </Box>
                <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                  {JSON.stringify(formattedData.data, null, 2)}
                </Typography>
              </CardContent>
            </Card>
          </Box>
        );

      default:
        return (
          <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
            {typeof formattedData.data === 'object' 
              ? JSON.stringify(formattedData.data, null, 2)
              : formattedData.data
            }
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
          <Button onClick={checkConnection} size="small" variant="outlined">
            🔄
          </Button>
        </Box>
      </Box>

      <Grid container spacing={2}>
        {/* Chat Area */}
        <Grid item xs={12} md={8}>
          <Card sx={{ height: '600px', display: 'flex', flexDirection: 'column' }}>
            <CardContent sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', p: 2 }}>
              {/* Messages Area */}
              <Box sx={{ flexGrow: 1, overflow: 'auto', mb: 2, height: '300px' }}>
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
              
              {/* Sample Questions */}
              <Box sx={{ mb: 1 }}>
                <Typography variant="caption" color="text.secondary" sx={{ mb: 0.5 }}>
                  Sample commands:
                </Typography>
                <Box display="flex" flexWrap="wrap" gap={0.5}>
                  {sampleCommands.map((command, index) => (
                    <Button
                      key={index}
                      variant="outlined"
                      size="small"
                      onClick={() => setInputValue(command)}
                      sx={{ fontSize: '0.7rem', py: 0.5, px: 1 }}
                    >
                      {command}
                    </Button>
                  ))}
                </Box>
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
                  Send 📤
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
