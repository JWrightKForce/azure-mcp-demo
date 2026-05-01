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

  const parseCostResponse = (text: string) => {
    const costMatch = text.match(/Total Cost: \$([0-9,]+\.\d+)/);
    const periodMatch = text.match(/Period: ([\d-]+) to ([\d-]+)/);
    const resourcesMatch = text.match(/Resources: (\d+)/);
    
    const resources: any[] = [];
    const lines = text.split('\n');
    let inResourcesSection = false;
    
    for (const line of lines) {
      if (line.includes('Top') && line.includes('Most Expensive')) {
        inResourcesSection = true;
        continue;
      }
      if (inResourcesSection && line.startsWith('-')) {
        const match = line.match(/- ([^(]+) \(([^)]+)\): \$([0-9,]+\.\d+)/);
        if (match) {
          resources.push({
            resourceName: match[1].trim(),
            resourceType: match[2],
            cost: parseFloat(match[3].replace(/,/g, ''))
          });
        }
      }
    }
    
    return {
      summary: `Total Cost: ${costMatch ? costMatch[1] : '0'} USD`,
      totalCost: costMatch ? parseFloat(costMatch[1].replace(/,/g, '')) : 0,
      currency: 'USD',
      periodStart: periodMatch ? periodMatch[1] : '',
      periodEnd: periodMatch ? periodMatch[2] : '',
      resources: resources
    };
  };

  const parseSecurityResponse = (text: string) => {
    const scoreMatch = text.match(/Security Score: (\d+)%/);
    const issuesMatch = text.match(/Issues: (\d+)/);
    const secureMatch = text.match(/Secure Resources: (\d+)/);
    const problemMatch = text.match(/Resources with Issues: (\d+)/);
    
    const score = scoreMatch ? parseInt(scoreMatch[1]) : 0;
    
    return {
      securityScore: score,
      assessment: score >= 80 ? 'Good' : score >= 60 ? 'Fair' : 'Poor',
      totalResources: 0,
      resourcesWithIssues: problemMatch ? parseInt(problemMatch[1]) : 0,
      issues: []
    };
  };

  const parseResourcesResponse = (text: string) => {
    const resourcesMatch = text.match(/Total Resources: (\d+)/);
    const groupsMatch = text.match(/Resource Groups: (\d+)/);
    const typesMatch = text.match(/Resource Types: (\d+)/);
    
    return {
      totalResources: resourcesMatch ? parseInt(resourcesMatch[1]) : 0,
      resourceGroups: groupsMatch ? parseInt(groupsMatch[1]) : 0,
      resourceTypes: typesMatch ? parseInt(typesMatch[1]) : 0,
      resources: []
    };
  };

  const formatResponse = (response: string, command: string) => {
    try {
      console.log('=== MCP Response ===');
      console.log('Raw response:', response);
      
      const mcpResponse = JSON.parse(response);
      console.log('Parsed MCP response:', mcpResponse);
      
      // The content is nested under result.content
      const content = mcpResponse.result?.content || mcpResponse.content;
      if (!content || !Array.isArray(content) || content.length === 0) {
        console.log('No content found in response');
        return { type: 'text', data: response };
      }

      const textContent = content[0].text;
      console.log('Text content:', textContent);
      console.log('Text content type:', typeof textContent);
      
      let toolOutput: any;
      try {
        toolOutput = JSON.parse(textContent);
        console.log('Parsed as JSON:', toolOutput);
      } catch (e) {
        console.log('Not JSON, parsing as plain text');
        // Parse the plain text response
        if (textContent.includes('Total Cost')) {
          toolOutput = parseCostResponse(textContent);
        } else if (textContent.includes('Security Score')) {
          toolOutput = parseSecurityResponse(textContent);
        } else if (textContent.includes('Total Resources')) {
          toolOutput = parseResourcesResponse(textContent);
        } else {
          toolOutput = { rawText: textContent };
        }
        console.log('Parsed plain text:', toolOutput);
      }
      
      const lowerCommand = command.toLowerCase();

      // Check if response contains cost data
      if (toolOutput.totalCost !== undefined || textContent.includes('Total Cost')) {
        return {
          type: 'cost',
          data: toolOutput
        };
      }

      if (lowerCommand.includes('audit') || lowerCommand.includes('security') || textContent.includes('Security Score')) {
        return {
          type: 'security',
          data: toolOutput
        };
      }

      // Check if response contains resource utilization data
      if (toolOutput.totalResources !== undefined || textContent.includes('Total Resources')) {
        return {
          type: 'resources',
          data: toolOutput,
          command: command
        };
      }

      if (lowerCommand.includes('backup')) {
        return {
          type: 'backup',
          data: toolOutput
        };
      }

      if (lowerCommand.includes('log') || lowerCommand.includes('query')) {
        return {
          type: 'logs',
          data: toolOutput
        };
      }

      return { type: 'text', data: textContent };
    } catch (error) {
      // If parsing fails at any stage, return the raw response as text
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
              <Grid item xs={12}>
                <Card variant="outlined">
                  <CardContent>
                    <Box display="flex" alignItems="center" mb={2}>
                      <Typography variant="h6" color="primary" sx={{ mr: 1 }}>💰</Typography>
                      <Typography variant="h6">Cost Analysis</Typography>
                    </Box>
                    <Box display="flex" justifyContent="space-between" alignItems="baseline" mb={2}>
                      <Box>
                        <Typography variant="h4" color="primary">
                          ${formattedData.data.totalCost?.toFixed(2) || '0.00'}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {formattedData.data.currency} • {formattedData.data.resources?.length || 0} resources
                        </Typography>
                      </Box>
                      {formattedData.data.periodStart && formattedData.data.periodEnd && (
                        <Typography variant="body2" color="text.secondary">
                          {new Date(formattedData.data.periodStart).toLocaleDateString()} to {new Date(formattedData.data.periodEnd).toLocaleDateString()}
                        </Typography>
                      )}
                    </Box>
                    <Typography variant="subtitle2" mb={1} fontWeight="bold">
                      Top {Math.min(10, formattedData.data.resources?.length || 0)} Cost Contributors
                    </Typography>
                    {formattedData.data.resources && formattedData.data.resources.length > 0 ? (
                      <>
                        {console.log('Top 10 resources:', formattedData.data.resources.slice(0, 10))}
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
                          {formattedData.data.resources.slice(0, 10).map((resource: any, index: number) => {
                            const percentage = ((resource.cost / (formattedData.data.resources?.reduce((sum: number, r: any) => sum + r.cost, 0) || 1)) * 100).toFixed(1);
                            return (
                              <Box key={index} sx={{ display: 'grid', gridTemplateColumns: '1fr 80px 60px', gap: 1, alignItems: 'center', py: 0.5, borderBottom: '1px solid #eee' }}>
                                <Box sx={{ minWidth: 0 }}>
                                  <Typography variant="body2" fontWeight="500" noWrap title={resource.resourceName}>
                                    {resource.resourceName}
                                  </Typography>
                                  <Typography variant="caption" color="text.secondary" noWrap>
                                    {resource.resourceType}
                                  </Typography>
                                </Box>
                                <Typography variant="body2" fontWeight="bold" color="primary" sx={{ textAlign: 'right' }}>
                                  ${resource.cost.toFixed(2)}
                                </Typography>
                                <Typography variant="caption" color="info.main" sx={{ textAlign: 'right' }}>
                                  {percentage}%
                                </Typography>
                              </Box>
                            );
                          })}
                        </Box>
                      </>
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
