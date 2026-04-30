const MCP_SERVER_URL = import.meta.env.VITE_MCP_SERVER_URL || 'http://localhost:5001/mcp';

// Mock responses for demo purposes
const mockResponses = {
  'Get resource costs': {
    TotalCost: 1247.89,
    Currency: 'USD',
    PeriodStart: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
    PeriodEnd: new Date().toISOString(),
    ResourceCosts: [
      {
        ResourceId: '/subscriptions/xxx/resourceGroups/production/providers/Microsoft.Web/sites/prod-webapp-01',
        ResourceName: 'prod-webapp-01',
        ResourceType: 'Microsoft.Web/sites',
        Cost: 456.23,
        Currency: 'USD'
      },
      {
        ResourceId: '/subscriptions/xxx/resourceGroups/production/providers/Microsoft.Sql/servers/prod-sql-01',
        ResourceName: 'prod-sql-01',
        ResourceType: 'Microsoft.Sql/servers',
        Cost: 234.56,
        Currency: 'USD'
      }
    ]
  },
  'Audit resources': {
    ScanTime: new Date().toISOString(),
    TotalResources: 47,
    SecureResources: 43,
    ResourcesWithIssues: 4,
    SecurityScore: 91.5,
    Issues: [
      {
        ResourceId: '/subscriptions/xxx/resourceGroups/production/providers/Microsoft.Storage/storageAccounts/insecurestorage',
        ResourceType: 'Microsoft.Storage/storageAccounts',
        IssueType: 'PublicAccessEnabled',
        Description: 'Storage account allows public access',
        Severity: 'High',
        Recommendation: 'Disable public access and use private endpoints',
        Resolved: false
      }
    ]
  }
};

class MCPService {
  async checkHealth() {
    try {
      const response = await fetch(`${MCP_SERVER_URL.replace('/mcp', '')}/health`);
      return await response.json();
    } catch (error) {
      // Mock health check for demo
      return { status: 'healthy', timestamp: new Date().toISOString() };
    }
  }

  async executeTool(command: string, addConsoleLog?: (message: string) => void) {
    const log = addConsoleLog || console.log;
    log('🚀 Starting MCP Protocol for command: ' + command);
    log('⏰ Timestamp: ' + new Date().toISOString());
    
    try {
      // Step 1: Initialize MCP session
      log('📡 Step 1: Initializing MCP session...');
      const initResponse = await fetch('http://localhost:5001/mcp', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          jsonrpc: '2.0',
          id: 1,
          method: 'initialize',
          params: {
            protocolVersion: '2024-11-05',
            capabilities: {
              tools: {}
            },
            clientInfo: {
              name: 'azure-devops-assistant-frontend',
              version: '1.0.0'
            }
          }
        })
      });

      if (!initResponse.ok) {
        console.error('❌ MCP initialization failed:', initResponse.status);
        log('❌ MCP initialization failed: ' + initResponse.status);
        throw new Error(`MCP initialization failed: ${initResponse.status}`);
      }

      const initResult = await initResponse.text();
      log('✅ MCP init response: ' + initResult);

      // Step 2: Send initialized notification
      log('📡 Step 2: Sending initialized notification...');
      const notifyResponse = await fetch('http://localhost:5001/mcp', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          jsonrpc: '2.0',
          method: 'notifications/initialized',
          params: {}
        })
      });

      log('✅ MCP notify response status: ' + notifyResponse.status);

      // Step 3: List tools to see what's available
      log('📡 Step 3: Listing available tools...');
      const toolsResponse = await fetch('http://localhost:5001/mcp', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          jsonrpc: '2.0',
          id: 2,
          method: 'tools/list',
          params: {}
        })
      });

      if (!toolsResponse.ok) {
        console.error('❌ MCP tools list failed:', toolsResponse.status);
        log('❌ MCP tools list failed: ' + toolsResponse.status);
        throw new Error(`MCP tools list failed: ${toolsResponse.status}`);
      }

      const toolsResult = await toolsResponse.text();
      log('✅ MCP tools list response: ' + toolsResult);

      // Step 4: Call the appropriate tool
      let toolName = 'GetResourceCosts';
      if (command.toLowerCase().includes('security') || command.toLowerCase().includes('audit') || command.toLowerCase().includes('vulnerabilities')) {
        toolName = 'AuditResources';
      } else if (command.toLowerCase().includes('cost') || command.toLowerCase().includes('costs') || command.toLowerCase().includes('billing') || command.toLowerCase().includes('price') || command.toLowerCase().includes('spending')) {
        toolName = 'GetResourceCosts'; // Use cost tool for cost-related queries
      } else if (command.toLowerCase().includes('database') || command.toLowerCase().includes('sql') || command.toLowerCase().includes('databases')) {
        toolName = 'GetResourceUtilization'; // Use existing tool with filter parameter
      } else if (command.toLowerCase().includes('resource') || command.toLowerCase().includes('resources') || command.toLowerCase().includes('what') || command.toLowerCase().includes('show me')) {
        toolName = 'GetResourceUtilization'; // Get resource list for discovery questions
      } else if (command.toLowerCase().includes('deploy')) {
        toolName = 'GetResourceUtilization'; // Fallback for deployment
      } else if (command.toLowerCase().includes('backup')) {
        toolName = 'GetResourceUtilization'; // Fallback for backup
      } else if (command.toLowerCase().includes('log') || command.toLowerCase().includes('query') || command.toLowerCase().includes('error')) {
        toolName = 'GetResourceUtilization'; // Fallback for logs
      }
      
      log(`📡 Step 4: Calling tool ${toolName}...`);
      log('🔄 Contacting MCP server for live data...');
      
      // Determine resource type filter for GetResourceUtilization
      let resourceTypeFilter = null;
      if (toolName === 'GetResourceUtilization') {
        if (command.toLowerCase().includes('database') || command.toLowerCase().includes('sql')) {
          resourceTypeFilter = 'Sql';
        } else if (command.toLowerCase().includes('web') || command.toLowerCase().includes('app')) {
          resourceTypeFilter = 'Web';
        } else if (command.toLowerCase().includes('storage')) {
          resourceTypeFilter = 'Storage';
        } else if (command.toLowerCase().includes('key') || command.toLowerCase().includes('vault')) {
          resourceTypeFilter = 'KeyVault';
        }
      }
      
      const requestBody = {
        jsonrpc: '2.0',
        id: 3,
        method: 'tools/call',
        params: {
          name: toolName,
          arguments: {
            resourceGroupName: null,
            ...(resourceTypeFilter && { resourceTypeFilter })
          }
        }
      };
      
            
      const response = await fetch('http://localhost:5001/mcp', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(requestBody)
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('❌ MCP tool call failed:', response.status, errorText);
        log('❌ MCP tool call failed: ' + response.status + ' - ' + errorText);
        throw new Error(`MCP tool call failed: ${response.status} - ${errorText}`);
      }

      const result = await response.text();
      log('✅ MCP tool result: ' + result);
      log('🎉 MCP Protocol Complete!');
      return result;
    } catch (error) {
      console.error('❌ Error in MCP protocol:', error);
      console.error('📍 Stack trace:', error.stack);
      log('❌ Error in MCP protocol: ' + (error instanceof Error ? error.message : 'Unknown error'));
      throw error;
    }
  }

  private async parseSSEOrJSON(response: Response): Promise<any> {
    const text = await response.text();
    console.log('Raw response text:', text);
    
    // Check if it's SSE format
    if (text.startsWith('event: message') || text.startsWith('data: ')) {
      // Parse SSE format
      const lines = text.split('\n');
      let sessionId = null;
      let jsonData = null;
      
      for (const line of lines) {
        if (line.startsWith('data: ')) {
          try {
            jsonData = JSON.parse(line.substring(6));
          } catch (e) {
            console.error('Failed to parse SSE data:', line);
          }
        }
        else if (line.startsWith('id: ')) {
          sessionId = line.substring(4);
        }
      }
      
      // Add session ID to the parsed data if found
      if (sessionId && jsonData) {
        jsonData.sessionId = sessionId;
      }
      
      console.log('Parsed SSE data:', jsonData);
      console.log('Session ID from SSE:', sessionId);
      
      return jsonData || (() => { throw new Error('No valid data found in SSE response'); })();
    }
    
    // Try parsing as JSON
    try {
      return JSON.parse(text);
    } catch (e) {
      console.error('Failed to parse JSON response:', text);
      throw new Error('Invalid response format');
    }
  }

  private getToolName(command: string): string {
    const lowerCommand = command.toLowerCase();
    
    if (lowerCommand.includes('cost') || lowerCommand.includes('resource costs')) {
      return 'GetResourceCosts';
    } else if (lowerCommand.includes('audit') || lowerCommand.includes('security')) {
      return 'AuditResources';
    } else if (lowerCommand.includes('deploy')) {
      return 'DeployArmTemplate';
    } else if (lowerCommand.includes('backup')) {
      return 'CreateBackup';
    } else if (lowerCommand.includes('log') || lowerCommand.includes('query')) {
      return 'QueryLogs';
    }
    
    return 'GetResourceCosts'; // default
  }
  
  private getMockResponse(command: string) {
    const lowerCommand = command.toLowerCase();
    
    if (lowerCommand.includes('cost') || lowerCommand.includes('resource costs')) {
      return JSON.stringify(mockResponses['Get resource costs'], null, 2);
    }
    
    if (lowerCommand.includes('audit') || lowerCommand.includes('security')) {
      return JSON.stringify(mockResponses['Audit resources'], null, 2);
    }
    
    if (lowerCommand.includes('deploy')) {
      return JSON.stringify({
        Success: true,
        DeploymentName: `deploy-${new Date().toISOString().slice(0, 19).replace(/[:-]/g, '')}`,
        Status: 'Succeeded',
        StartTime: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
        EndTime: new Date().toISOString(),
        DeployedResources: [
          {
            ResourceId: '/subscriptions/xxx/resourceGroups/production/providers/Microsoft.Web/sites/new-webapp',
            ResourceName: 'new-webapp',
            ResourceType: 'Microsoft.Web/sites',
            Status: 'Succeeded'
          }
        ]
      }, null, 2);
    }
    
    if (lowerCommand.includes('backup')) {
      return JSON.stringify({
        Success: true,
        BackupId: `backup-${new Date().toISOString().slice(0, 19).replace(/[:-]/g, '')}`,
        Status: 'Completed',
        StartTime: new Date(Date.now() - 10 * 60 * 1000).toISOString(),
        EndTime: new Date().toISOString(),
        SizeInMB: 1024
      }, null, 2);
    }
    
    if (lowerCommand.includes('log') || lowerCommand.includes('query')) {
      return JSON.stringify({
        Query: command,
        QueryTime: new Date().toISOString(),
        TotalRecords: 15,
        Entries: [
          {
            Timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
            Level: 'Error',
            Message: 'Database connection timeout',
            ResourceId: '/subscriptions/xxx/resourceGroups/production/providers/Microsoft.Web/sites/prod-webapp-01',
            ResourceType: 'Microsoft.Web/sites'
          }
        ]
      }, null, 2);
    }
    
    return JSON.stringify({
      message: `Command executed: ${command}`,
      timestamp: new Date().toISOString()
    }, null, 2);
  }

  async getResourceCosts() {
    return mockResponses['Get resource costs'];
  }

  async getSecurityAudit() {
    return mockResponses['Audit resources'];
  }

  async getDeployments() {
    return {
      Deployments: [
        {
          DeploymentName: 'deploy-20240129-001',
          Status: 'Succeeded',
          Timestamp: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
          Template: 'ARM Template',
          CorrelationId: '12345678-1234-1234-1234-123456789012'
        },
        {
          DeploymentName: 'deploy-20240128-002',
          Status: 'Failed',
          Timestamp: new Date(Date.now() - 48 * 60 * 60 * 1000).toISOString(),
          Template: 'ARM Template',
          CorrelationId: '87654321-4321-4321-4321-210987654321'
        }
      ]
    };
  }

  async getBackups() {
    return {
      TotalBackups: 5,
      BackupsByType: [
        {
          BackupType: 'Full',
          Count: 2,
          Items: [
            {
              BackupId: 'backup-20240129-full-001',
              CreatedTime: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
              SizeInMB: 2048,
              Status: 'Completed',
              ExpiryTime: new Date(Date.now() + 28 * 24 * 60 * 60 * 1000).toISOString()
            }
          ]
        },
        {
          BackupType: 'Incremental',
          Count: 3,
          Items: [
            {
              BackupId: 'backup-20240129-inc-001',
              CreatedTime: new Date(Date.now() - 1 * 60 * 60 * 1000).toISOString(),
              SizeInMB: 512,
              Status: 'Completed',
              ExpiryTime: new Date(Date.now() + 29 * 24 * 60 * 60 * 1000).toISOString()
            }
          ]
        }
      ]
    };
  }
}

export default new MCPService();
