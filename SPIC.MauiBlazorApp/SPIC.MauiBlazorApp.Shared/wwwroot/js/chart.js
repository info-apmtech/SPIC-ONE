        // Define globally to ensure Blazor can call it via JSInterop
        window.loadSalesCharts = function() {
        
        // Ensure Chart.js is loaded
        if (typeof Chart === 'undefined') return;

        // Custom plugin to draw the faint background track behind the bars
        const barBackgroundPlugin = {
        id: 'barBackground',
        beforeDatasetsDraw(chart) {
        if (chart.config.type !== 'bar') return;
        const { ctx, chartArea: { top, bottom } } = chart;
        ctx.save();
        ctx.fillStyle = 'rgba(243, 232, 255, 0.5)'; // Faint purple track
        chart.getDatasetMeta(0).data.forEach((bar) => {
        const width = bar.width;
        const xPos = bar.x - width / 2;
        ctx.beginPath();
        if(ctx.roundRect) {
        ctx.roundRect(xPos, top, width, bottom - top, [6, 6, 0, 0]);
        } else {
        ctx.rect(xPos, top, width, bottom - top);
        }
        ctx.fill();
        });
        ctx.restore();
        }
        };

        // Custom plugin to draw the dashed crosshair line on the Area chart
        const verticalLinePlugin = {
        id: 'verticalLine',
        afterDraw: chart => {
        if (chart.config.type !== 'line') return;
        if (chart.tooltip?._active && chart.tooltip._active.length) {
        const activePoint = chart.tooltip._active[0];
        const ctx = chart.ctx;
        const x = activePoint.element.x;
        const topY = chart.scales.y.top;
        const bottomY = chart.scales.y.bottom;

        ctx.save();
        ctx.beginPath();
        ctx.moveTo(x, topY);
        ctx.lineTo(x, bottomY);
        ctx.lineWidth = 1;
        ctx.strokeStyle = '#cbd5e1'; // Slate color
        ctx.setLineDash([5, 5]);
        ctx.stroke();
        ctx.restore();
        }
        }
        };

        // Register custom plugins
        Chart.register(barBackgroundPlugin, verticalLinePlugin);

        // --- 1. Bar Chart (Product-wise Sales Distribution) ---
        const barCanvas = document.getElementById('barChart');
        if (barCanvas) {
        const ctxBar = barCanvas.getContext('2d');
        const gradientPurple = ctxBar.createLinearGradient(0, 0, 0, 300);
        gradientPurple.addColorStop(0, 'rgba(192, 132, 252, 0.9)');
        gradientPurple.addColorStop(1, 'rgba(216, 180, 254, 0.4)');

        if (window.myBarChart) window.myBarChart.destroy();

        window.myBarChart = new Chart(ctxBar, {
        type: 'bar',
        data: {
        labels: [
        ['Neem Coated', 'Urea(45 Kg)'],
        ['Neem Coated', 'Urea(45 Kg)'],
        ['Neem Coated', 'Urea(45 Kg)'],
        ['Neem Coated', 'Urea(45 Kg)'],
        ['Neem Coated', 'Urea(45 Kg)'],
        ['Neem Coated', 'Urea(45 Kg)']
        ],
        datasets: [{
        label: 'Total Sales (units)',
        data: [2400, 4800, 1200, 8500, 4500, 2200],
        backgroundColor: gradientPurple,
        borderRadius: { topLeft: 6, topRight: 6, bottomLeft: 0, bottomRight: 0 },
        barPercentage: 0.5,
        categoryPercentage: 0.8
        }]
        },
        options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
        legend: { display: false },
        tooltip: {
        backgroundColor: '#ffffff',
        titleColor: '#9333ea',
        titleFont: { size: 13, weight: 'bold' },
        bodyColor: '#4b5563',
        borderColor: '#ddd6fe',
        borderWidth: 1,
        padding: 12,
        boxShadow: '0 4px 6px rgba(0,0,0,0.1)',
        displayColors: false,
        callbacks: {
        title: function(context) { return context[0].label.replace(',', ' '); },
        label: function(context) {
        return [
        `Total Sales (units): ${context.raw}`,
        `Average Liquidation (Days): 91`
        ];
        }
        }
        }
        },
        scales: {
        y: {
        beginAtZero: true,
        max: 10000,
        border: { display: false },
        grid: { color: '#f8fafc', borderDash: [5, 5] },
        ticks: { color: '#94a3b8', font: {size: 10}, stepSize: 2500 }
        },
        x: {
        border: { display: false },
        grid: { display: false },
        ticks: { color: '#94a3b8', font: {size: 10} }
        }
        }
        }
        });
        }

        // --- 2. Area Chart (District-wise Sales Trend) ---
        const areaCanvas = document.getElementById('areaChart');
        if(areaCanvas) {
        const ctxArea = areaCanvas.getContext('2d');
            
        const gradientGreen = ctxArea.createLinearGradient(0, 0, 0, 300);
        gradientGreen.addColorStop(0, 'rgba(74, 222, 128, 0.4)');
        gradientGreen.addColorStop(1, 'rgba(74, 222, 128, 0.0)');

        const gradientBlue = ctxArea.createLinearGradient(0, 0, 0, 300);
        gradientBlue.addColorStop(0, 'rgba(56, 189, 248, 0.5)');
        gradientBlue.addColorStop(1, 'rgba(56, 189, 248, 0.0)');

        if (window.myAreaChart) window.myAreaChart.destroy();

        window.myAreaChart = new Chart(ctxArea, {
        type: 'line',
        data: {
        labels: ['Coimbatore', 'Madurai', 'Chennai', 'Salem', 'Tuticorin', 'Dindigul'],
        datasets: [
        {
        label: 'Trends %',
        data: [1500, 1300, 1450, 1600, 1500, 1200],
        borderColor: '#22c55e',
        backgroundColor: gradientGreen,
        borderWidth: 2,
        fill: true,
        tension: 0.4,
        pointRadius: 0,
        pointHoverRadius: 6,
        pointBackgroundColor: '#ffffff',
        pointBorderColor: '#22c55e',
        pointBorderWidth: 2
        },
        {
        label: 'Sales (units)',
        data: [1100, 1400, 880, 1150, 1200, 850],
        borderColor: '#3b82f6',
        backgroundColor: gradientBlue,
        borderWidth: 2,
        fill: true,
        tension: 0.4,
        pointRadius: 0,
        pointHoverRadius: 6,
        pointBackgroundColor: '#ffffff',
        pointBorderColor: '#3b82f6',
        pointBorderWidth: 2
        }
        ]
        },
        options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: {
        mode: 'index',
        intersect: false,
        },
        plugins: {
        legend: { display: false },
        tooltip: {
        backgroundColor: '#ffffff',
        titleColor: '#334155',
        bodyColor: '#64748b',
        borderColor: '#e2e8f0',
        borderWidth: 1,
        padding: 12,
        usePointStyle: true,
        boxPadding: 6,
        callbacks: {
        title: function(context) { return context[0].label; },
        label: function(context) {
        let label = context.dataset.label || '';
        if (label) { label += ': '; }
        if (context.parsed.y !== null) { label += context.parsed.y; }
        return label;
        }
        }
        }
        },
        scales: {
        y: {
        beginAtZero: true,
        max: 2000,
        border: { display: false },
        grid: { color: '#f8fafc', borderDash: [5, 5] },
        ticks: { color: '#94a3b8', font: {size: 10}, stepSize: 750 }
        },
        x: {
        border: { display: false },
        grid: { display: true, color: '#f8fafc', borderDash: [5, 5] },
        ticks: { color: '#94a3b8', font: {size: 10} }
        }
        }
        }
        });
        }
        };

        // Global function for Ageing Report Charts
        window.loadAgeingCharts = function() {
        if (typeof Chart === 'undefined') return;

        // Custom plugin for faint background bars
        const barBackgroundPlugin = {
        id: 'barBackground',
        beforeDatasetsDraw(chart) {
        if (chart.config.type !== 'bar') return;
        const { ctx, chartArea: { top, bottom } } = chart;
        ctx.save();
        ctx.fillStyle = 'rgba(243, 232, 255, 0.5)';
        chart.getDatasetMeta(0).data.forEach((bar) => {
        const width = bar.width;
        const xPos = bar.x - width / 2;
        ctx.beginPath();
        if(ctx.roundRect) ctx.roundRect(xPos, top, width, bottom - top, [6, 6, 0, 0]);
        else ctx.rect(xPos, top, width, bottom - top);
        ctx.fill();
        });
        ctx.restore();
        }
        };

        // Custom plugin for Donut Center Text
        const centerTextPlugin = {
        id: 'centerText',
        beforeDraw: function(chart) {
        if (chart.config.type !== 'doughnut') return;
        var width = chart.width, height = chart.height, ctx = chart.ctx;
        ctx.restore();
        var fontSize = (height / 114).toFixed(2);
        ctx.font = "bold " + fontSize + "em 'Inter', sans-serif";
        ctx.textBaseline = "middle";
        ctx.fillStyle = "#1e293b";
        var text = "25,681", textX = Math.round((width - ctx.measureText(text).width) / 2), textY = height / 2 - 10;
        ctx.fillText(text, textX, textY);

        ctx.font = "600 " + (fontSize * 0.35) + "em 'Inter', sans-serif";
        ctx.fillStyle = "#64748b";
        var text2 = "Total Stock (MT)", text2X = Math.round((width - ctx.measureText(text2).width) / 2), text2Y = height / 2 + 15;
        ctx.fillText(text2, text2X, text2Y);
        ctx.save();
        }
        };

        // Register custom plugins safely
        Chart.register(barBackgroundPlugin, centerTextPlugin);

        // --- 1. Bar Chart (Ageing by State) ---
        const barCanvas = document.getElementById('ageingBarChart');
        if (barCanvas) {
        const ctxBar = barCanvas.getContext('2d');
        const gradientPurple = ctxBar.createLinearGradient(0, 0, 0, 300);
        gradientPurple.addColorStop(0, 'rgba(192, 132, 252, 0.9)');
        gradientPurple.addColorStop(1, 'rgba(216, 180, 254, 0.4)');

        if (window.myAgeingBarChart) window.myAgeingBarChart.destroy();

        window.myAgeingBarChart = new Chart(ctxBar, {
        type: 'bar',
        data: {
        labels: ['Tamil Nadu', 'Maharashtra', 'Karnataka', 'Andhra Pradesh', 'Gujarat', 'Others'],
        datasets: [{
        label: 'Stock',
        data: [10000, 8000, 6100, 5000, 3600, 3100],
        backgroundColor: gradientPurple,
        borderRadius: { topLeft: 6, topRight: 6, bottomLeft: 0, bottomRight: 0 },
        barPercentage: 0.5,
        categoryPercentage: 0.8
        }]
        },
        options: {
        responsive: true, maintainAspectRatio: false,
        plugins: {
        legend: { display: false },
        tooltip: {
        backgroundColor: '#ffffff', titleColor: '#9333ea', titleFont: { size: 13, weight: 'bold' },
        bodyColor: '#4b5563', borderColor: '#ddd6fe', borderWidth: 1, padding: 12, boxShadow: '0 4px 6px rgba(0,0,0,0.1)', displayColors: false,
        callbacks: {
        title: function() { return "Neem Coated Urea(45 Kg)"; },
        label: function(context) {
        return [
        `Total Sales (units): 8451`,
        `Average Liquidation (Days): 91`
        ];
        }
        }
        }
        },
        scales: {
        y: { beginAtZero: true, max: 10000, border: { display: false }, grid: { color: '#f8fafc', borderDash: [5, 5] }, ticks: { color: '#94a3b8', font: {size: 10}, stepSize: 2500 } },
        x: { border: { display: false }, grid: { display: false }, ticks: { color: '#94a3b8', font: {size: 10} } }
        }
        }
        });
        }

        // --- 2. Donut Chart (Ageing Table) ---
        const donutCanvas = document.getElementById('ageingDonutChart');
        if (donutCanvas) {
        const ctxDonut = donutCanvas.getContext('2d');
        if (window.myAgeingDonutChart) window.myAgeingDonutChart.destroy();

        window.myAgeingDonutChart = new Chart(ctxDonut, {
        type: 'doughnut',
        data: {
        labels: ['0 - 15 Days', '16 - 30 Days', '31 - 60 Days', '60+ Days'],
        datasets: [{
        data: [12400, 300, 7600, 5251],
        backgroundColor: ['#047857', '#65a30d', '#ea580c', '#ef4444'],
        borderWidth: 0,
        hoverOffset: 4
        }]
        },
        options: {
        responsive: true, maintainAspectRatio: false,
        cutout: '75%',
        plugins: {
        legend: { display: false },
        tooltip: {
        backgroundColor: '#ffffff', titleColor: '#16a34a', titleFont: { size: 13, weight: 'bold' },
        bodyColor: '#475569', borderColor: '#e2e8f0', borderWidth: 1, padding: 12, displayColors: false,
        callbacks: {
        label: function(context) {
        const value = context.raw.toLocaleString();
        const total = context.chart._metasets[context.datasetIndex].total;
        const percentage = ((context.raw / total) * 100).toFixed(1) + '%';
        return [`Stock: ${value}`, `% of Total: ${percentage}`];
        }
        }
        }
        }
        }
        });
        }
};

window.loadRMDDashboardCharts = function () {
    if (typeof Chart === 'undefined') return;

    // ── 1. Analytics Validation Trend (Grouped Bar) ──
    const analyticsCanvas = document.getElementById('rmdAnalyticsChart');
    if (analyticsCanvas) {
        if (window.rmdAnalyticsChartInstance) window.rmdAnalyticsChartInstance.destroy();
        window.rmdAnalyticsChartInstance = new Chart(analyticsCanvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: ['Nov', 'Dec', 'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
                datasets: [
                    {
                        label: 'Submitted',
                        data: [55, 45, 50, 38, 40, 55, 38, 55],
                        backgroundColor: '#3B82F6',
                        borderRadius: { topLeft: 4, topRight: 4, bottomLeft: 0, bottomRight: 0 },
                        barPercentage: 0.7,
                        categoryPercentage: 0.7
                    },
                    {
                        label: 'Validated',
                        data: [50, 40, 45, 28, 50, 50, 28, 50],
                        backgroundColor: '#22C55E',
                        borderRadius: { topLeft: 4, topRight: 4, bottomLeft: 0, bottomRight: 0 },
                        barPercentage: 0.7,
                        categoryPercentage: 0.7
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#ffffff',
                        titleColor: '#111827',
                        titleFont: { size: 13, weight: 'bold' },
                        bodyColor: '#4b5563',
                        borderColor: '#e5e7eb',
                        borderWidth: 1,
                        padding: 12,
                        usePointStyle: true,
                        boxPadding: 6
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        max: 70,
                        border: { display: false },
                        grid: { color: '#f3f4f6' },
                        ticks: { color: '#9ca3af', font: { size: 10 }, stepSize: 10 }
                    },
                    x: {
                        border: { display: false },
                        grid: { display: false },
                        ticks: { color: '#9ca3af', font: { size: 11 } }
                    }
                }
            }
        });
    }

    // ── 2. RM Approval Status (Concentric Rings) ──
    const approvalCanvas = document.getElementById('rmdApprovalChart');
    if (approvalCanvas) {
        if (window.rmdApprovalChartInstance) window.rmdApprovalChartInstance.destroy();
        const track = '#F1F5F9';
        const total = 50;
        window.rmdApprovalChartInstance = new Chart(approvalCanvas.getContext('2d'), {
            type: 'doughnut',
            data: {
                datasets: [
                    { data: [16, total - 16], backgroundColor: ['#10B981', track], borderWidth: 0, weight: 1 },  // Approved (outer)
                    { data: [8, total - 8], backgroundColor: ['#F59E0B', track], borderWidth: 0, weight: 1 },  // Pending
                    { data: [3, total - 3], backgroundColor: ['#EF4444', track], borderWidth: 0, weight: 1 },  // Rejected
                    { data: [23, total - 23], backgroundColor: ['#9CA3AF', track], borderWidth: 0, weight: 1 }   // Yet to Send (inner)
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '50%',
                plugins: {
                    legend: { display: false },
                    tooltip: { enabled: false }
                }
            }
        });
    }

    // ── 3. RMD Validation Due Status (Donut) ──
    const dueCanvas = document.getElementById('rmdDueChart');
    if (dueCanvas) {
        if (window.rmdDueChartInstance) window.rmdDueChartInstance.destroy();
        window.rmdDueChartInstance = new Chart(dueCanvas.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: ['Due Today', 'Due in 2 days', 'Over Due', 'Auto-Forward risk'],
                datasets: [{
                    data: [7, 4, 6, 3],
                    backgroundColor: ['#FCA5A5', '#FCD34D', '#F87171', '#C4B5FD'],
                    borderWidth: 0,
                    hoverOffset: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '65%',
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#ffffff',
                        titleColor: '#111827',
                        bodyColor: '#4b5563',
                        borderColor: '#e5e7eb',
                        borderWidth: 1,
                        padding: 10
                    }
                }
            }
        });
    }

    // ── 4. Budget Utilization (Horizontal Bars) ──
    const budgetCanvas = document.getElementById('rmdBudgetChart');
    if (budgetCanvas) {
        if (window.rmdBudgetChartInstance) window.rmdBudgetChartInstance.destroy();
        window.rmdBudgetChartInstance = new Chart(budgetCanvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: ['Chennai HQ', 'Coimbatore HQ', 'Madurai HQ', 'Trichy HQ', 'Salem HQ'],
                datasets: [{
                    data: [78, 16, 58, 42, 35],
                    backgroundColor: ['#10B981', '#3B82F6', '#A78BFA', '#F59E0B', '#06B6D4'],
                    borderRadius: 6,
                    barPercentage: 0.55,
                    categoryPercentage: 0.8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                indexAxis: 'y',
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#ffffff',
                        titleColor: '#111827',
                        bodyColor: '#4b5563',
                        borderColor: '#e5e7eb',
                        borderWidth: 1,
                        padding: 10,
                        displayColors: false,
                        callbacks: {
                            label: function (ctx) { return `Utilization: ${ctx.raw}%`; }
                        }
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        max: 100,
                        border: { display: false },
                        grid: { color: '#f3f4f6' },
                        ticks: {
                            color: '#9ca3af',
                            font: { size: 10 },
                            stepSize: 20,
                            callback: function (v) { return v + '%'; }
                        }
                    },
                    y: {
                        border: { display: false },
                        grid: { display: false },
                        ticks: { color: '#374151', font: { size: 12 } }
                    }
                }
            }
        });
    }
};
