/**
 * TimeAxis Component - Usage Examples
 * 
 * This file demonstrates how to use the TimeAxis component with:
 * 1. Custom canvas visualizations (like Spectrogram)
 * 2. Mantine Charts (LineChart, AreaChart, etc.)
 */

import { Paper, Box } from '@mantine/core';
import { LineChart, AreaChart } from '@mantine/charts';
import TimeAxis from './TimeAxis';

// ============================================================================
// Example 1: Using TimeAxis with a custom canvas visualization
// ============================================================================
function CustomVisualizationExample() {
  const durationMs = 180000; // 3 minutes

  return (
    <Paper p="md">
      <Box style={{ position: 'relative', width: '100%', height: 250 }}>
        <canvas width={800} height={250} style={{ width: '100%', height: '100%' }} />
      </Box>
      {/* Add TimeAxis below your visualization */}
      <TimeAxis durationMs={durationMs} />
    </Paper>
  );
}

// ============================================================================
// Example 2: Using TimeAxis with Mantine LineChart
// ============================================================================
function MantineLineChartExample() {
  const durationMs = 240000; // 4 minutes
  
  // Sample data - convert time to formatted strings for Mantine Charts
  const chartData = [
    { time: '0:00', value: 128 },
    { time: '1:00', value: 192 },
    { time: '2:00', value: 256 },
    { time: '3:00', value: 224 },
    { time: '4:00', value: 192 },
  ];

  return (
    <Paper p="md">
      <LineChart
        h={200}
        data={chartData}
        dataKey="time"
        series={[{ name: 'value', label: 'Bitrate', color: 'blue.5' }]}
        curveType="monotone"
        withDots={false}
        xAxisProps={{ 
          tickMargin: 10,
          // Hide default x-axis labels since we're using TimeAxis
          tick: false,
        }}
      />
      {/* Add TimeAxis below the chart */}
      <TimeAxis durationMs={durationMs} />
    </Paper>
  );
}

// ============================================================================
// Example 3: Using TimeAxis with Mantine AreaChart
// ============================================================================
function MantineAreaChartExample() {
  const durationMs = 600000; // 10 minutes
  
  const chartData = [
    { time: '0:00', left: 80, right: 82 },
    { time: '2:00', left: 75, right: 78 },
    { time: '4:00', left: 85, right: 83 },
    { time: '6:00', left: 78, right: 80 },
    { time: '8:00', left: 82, right: 84 },
    { time: '10:00', left: 80, right: 81 },
  ];

  return (
    <Paper p="md">
      <AreaChart
        h={180}
        data={chartData}
        dataKey="time"
        series={[
          { name: 'left', label: 'Left Channel', color: 'blue.6' },
          { name: 'right', label: 'Right Channel', color: 'pink.6' },
        ]}
        curveType="monotone"
        withDots={false}
        xAxisProps={{ 
          tickMargin: 10,
          // Hide default x-axis labels
          tick: false,
        }}
        fillOpacity={0.5}
      />
      {/* Add TimeAxis with custom interval */}
      <TimeAxis durationMs={durationMs} intervalSeconds={120} />
    </Paper>
  );
}

// ============================================================================
// Example 4: TimeAxis with custom styling options
// ============================================================================
function CustomizedTimeAxisExample() {
  const durationMs = 45000; // 45 seconds

  return (
    <Paper p="md">
      <Box style={{ position: 'relative', width: '100%', height: 100, background: '#1a1b1e' }} />
      {/* TimeAxis with custom height and no tick marks */}
      <TimeAxis 
        durationMs={durationMs} 
        height={25}
        showTicks={false}
      />
    </Paper>
  );
}

// ============================================================================
// Adaptive Interval Behavior
// ============================================================================
/**
 * The TimeAxis component automatically adjusts label spacing based on duration:
 * 
 * - 0-30 seconds:    Every 5 seconds
 * - 30-60 seconds:   Every 10 seconds
 * - 1-2 minutes:     Every 15 seconds
 * - 2-5 minutes:     Every 30 seconds
 * - 5-10 minutes:    Every 1 minute
 * - 10-30 minutes:   Every 2 minutes
 * - 30+ minutes:     Every 5 minutes
 * 
 * You can override this by providing the `intervalSeconds` prop.
 */

export {
  CustomVisualizationExample,
  MantineLineChartExample,
  MantineAreaChartExample,
  CustomizedTimeAxisExample,
};

