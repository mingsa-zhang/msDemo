using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DataAgentRetryTool.Models;
using DataAgentRetryTool.Services;
using DataAgentRetryTool.Workers;

namespace DataAgentRetryTool;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService;
    private readonly DatabaseService _databaseService;

    // 正式环境
    private CancellationTokenSource? _productionCancellationTokenSource;
    private ProductionRetryWorker? _productionWorker;
    private RetryStats _productionStats = new();

    // 测试环境
    private CancellationTokenSource? _testCancellationTokenSource;
    private TestRetryWorker? _testWorker;
    private RetryStats _testStats = new();

    // 测试环境按时间条件查询
    private List<TransmissionRecord> _testTimeRecords = new();
    private CancellationTokenSource? _testTimeCancellationTokenSource;
    private bool _isTestTimeRunning = false;

    // 放弃传输记录
    private List<TransmissionRecord> _abandonRecords = new();
    private CancellationTokenSource? _abandonCancellationTokenSource;
    private bool _isAbandonRunning = false;

    public MainWindow()
    {
        InitializeComponent();
        _configService = new ConfigService();
        _databaseService = new DatabaseService();

        // 加载保存的Token
        LoadSavedTokens();
    }

    /// <summary>
    /// 加载保存的Token
    /// </summary>
    private void LoadSavedTokens()
    {
        // 正式环境Token
        var productionToken = _configService.LoadToken(EnvironmentType.Production);
        if (!string.IsNullOrEmpty(productionToken))
        {
            ProductionTokenTextBox.Text = productionToken;
        }

        // 测试环境Token
        var testToken = _configService.LoadToken(EnvironmentType.Test);
        if (!string.IsNullOrEmpty(testToken))
        {
            TestTokenTextBox.Text = testToken;
        }
    }

    #region 正式环境事件

    /// <summary>
    /// 正式环境 - 查询按钮点击
    /// </summary>
    private async void ProductionQueryButton_Click(object sender, RoutedEventArgs e)
    {
        var token = ProductionTokenTextBox.Text.Trim();
        if (string.IsNullOrEmpty(token))
        {
            MessageBox.Show("请输入授权Token", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 保存Token
        _configService.SaveToken(token, EnvironmentType.Production);

        ProductionQueryButton.IsEnabled = false;
        ProductionStatusTextBlock.Text = "正在查询数据...";

        try
        {
            var service = new DataTransmissionService();
            service.SetEnvironment(EnvironmentType.Production);
            service.SetToken(token);

            var result = await service.QueryProductionRecordsAsync(1, 10);
            if (result != null && result.IsSuccess)
            {
                ProductionCountTextBlock.Text = $"总记录数: {result.Count}";
                AppendProductionLog($"[信息] 总记录数: {result.Count}");
                _productionStats.TotalCount = result.Count;
                ProductionStartRetryButton.IsEnabled = true;
            }
            else
            {
                AppendProductionLog($"[错误] 查询失败: {result?.Msg ?? "无响应"}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ProductionQueryButton.IsEnabled = true;
            ProductionStatusTextBlock.Text = "查询完成";
        }
    }

    /// <summary>
    /// 正式环境 - 开始自动重推
    /// </summary>
    private async void ProductionStartRetryButton_Click(object sender, RoutedEventArgs e)
    {
        var token = _configService.LoadToken(EnvironmentType.Production);

        _productionCancellationTokenSource = new CancellationTokenSource();
        ProductionStartRetryButton.IsEnabled = false;
        ProductionStopRetryButton.IsEnabled = true;
        ProductionQueryButton.IsEnabled = false;

        // 清空日志
        ProductionLogTextBox.Clear();

        // 重置统计
        _productionStats = new RetryStats();

        // 创建Worker
        var service = new DataTransmissionService();
        service.SetEnvironment(EnvironmentType.Production);
        service.SetToken(token);

        _productionWorker = new ProductionRetryWorker(service, _databaseService);
        _productionWorker.LogAppended += OnProductionLogAppended;
        _productionWorker.StatsUpdated += OnProductionStatsUpdated;

        try
        {
            await _productionWorker.RunAsync(_productionCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // 忽略取消异常
        }
        finally
        {
            ProductionStartRetryButton.IsEnabled = true;
            ProductionStopRetryButton.IsEnabled = false;
            ProductionQueryButton.IsEnabled = true;
            _productionWorker = null;
        }
    }

    /// <summary>
    /// 正式环境 - 停止重推
    /// </summary>
    private void ProductionStopRetryButton_Click(object sender, RoutedEventArgs e)
    {
        _productionCancellationTokenSource?.Cancel();
        ProductionStopRetryButton.IsEnabled = false;
        AppendProductionLog("[信息] 正在停止任务...");
    }

    /// <summary>
    /// 正式环境 - 清空日志
    /// </summary>
    private void ProductionClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        ProductionLogTextBox.Clear();
    }

    /// <summary>
    /// 正式环境 - 日志追加回调
    /// </summary>
    private void OnProductionLogAppended(string message)
    {
        Dispatcher.Invoke(() =>
        {
            ProductionLogTextBox.AppendText($"{message}\n");
            ProductionLogTextBox.ScrollToEnd();
        });
    }

    /// <summary>
    /// 正式环境 - 统计更新回调
    /// </summary>
    private void OnProductionStatsUpdated(RetryStats stats)
    {
        Dispatcher.Invoke(() =>
        {
            ProductionStatInit.Text = $"初次:{stats.InitialTotalCount}";
            ProductionStat.Text = $"剩余:{stats.TotalCount} 成功:{stats.SuccessCount} 跳过:{stats.SkipCount} 失败:{stats.FailCount} 取消:{stats.CancelledCount}";
        });
    }

    /// <summary>
    /// 正式环境 - 追加日志
    /// </summary>
    private void AppendProductionLog(string message)
    {
        OnProductionLogAppended(message);
    }

    #endregion

    #region 测试环境事件

    /// <summary>
    /// 测试环境 - 查询按钮点击
    /// </summary>
    private async void TestQueryButton_Click(object sender, RoutedEventArgs e)
    {
        var token = TestTokenTextBox.Text.Trim();
        if (string.IsNullOrEmpty(token))
        {
            MessageBox.Show("请输入授权Token", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 保存Token
        _configService.SaveToken(token, EnvironmentType.Test);

        TestQueryButton.IsEnabled = false;
        TestStatusTextBlock.Text = "正在查询数据...";

        try
        {
            var service = new DataTransmissionService();
            service.SetEnvironment(EnvironmentType.Test);
            service.SetToken(token);

            var result = await service.QueryTestRecordsAsync(1, 10);
            if (result != null && result.IsSuccess)
            {
                TestCountTextBlock.Text = $"总记录数: {result.Count}";
                AppendTestLog($"[信息] 总记录数: {result.Count}");
                _testStats.TotalCount = result.Count;
                TestStartRetryButton.IsEnabled = true;
            }
            else
            {
                AppendTestLog($"[错误] 查询失败: {result?.Msg ?? "无响应"}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            TestQueryButton.IsEnabled = true;
            TestStatusTextBlock.Text = "查询完成";
        }
    }

    /// <summary>
    /// 测试环境 - 开始自动重推
    /// </summary>
    private async void TestStartRetryButton_Click(object sender, RoutedEventArgs e)
    {
        var token = _configService.LoadToken(EnvironmentType.Test);

        _testCancellationTokenSource = new CancellationTokenSource();
        TestStartRetryButton.IsEnabled = false;
        TestStopRetryButton.IsEnabled = true;
        TestQueryButton.IsEnabled = false;

        // 清空日志
        TestLogTextBox.Clear();

        // 重置统计
        _testStats = new RetryStats();

        // 创建Worker
        var service = new DataTransmissionService();
        service.SetEnvironment(EnvironmentType.Test);
        service.SetToken(token);

        _testWorker = new TestRetryWorker(service, _databaseService);
        _testWorker.LogAppended += OnTestLogAppended;
        _testWorker.StatsUpdated += OnTestStatsUpdated;

        try
        {
            await _testWorker.RunAsync(_testCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // 忽略取消异常
        }
        finally
        {
            TestStartRetryButton.IsEnabled = true;
            TestStopRetryButton.IsEnabled = false;
            TestQueryButton.IsEnabled = true;
            _testWorker = null;
        }
    }

    /// <summary>
    /// 测试环境 - 停止重推
    /// </summary>
    private void TestStopRetryButton_Click(object sender, RoutedEventArgs e)
    {
        _testCancellationTokenSource?.Cancel();
        TestStopRetryButton.IsEnabled = false;
        AppendTestLog("[信息] 正在停止任务...");
    }

    /// <summary>
    /// 测试环境 - 清空日志
    /// </summary>
    private void TestClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        TestLogTextBox.Clear();
    }

    /// <summary>
    /// 测试环境 - 日志追加回调
    /// </summary>
    private void OnTestLogAppended(string message)
    {
        Dispatcher.Invoke(() =>
        {
            TestLogTextBox.AppendText($"{message}\n");
            TestLogTextBox.ScrollToEnd();
        });
    }

    /// <summary>
    /// 测试环境 - 统计更新回调
    /// </summary>
    private void OnTestStatsUpdated(RetryStats stats)
    {
        Dispatcher.Invoke(() =>
        {
            TestStatInit.Text = $"初次:{stats.InitialTotalCount}";
            TestStat.Text = $"剩余:{stats.TotalCount} 成功:{stats.SuccessCount} 跳过:{stats.SkipCount} 失败:{stats.FailCount} 取消:{stats.CancelledCount}";
        });
    }

    /// <summary>
    /// 测试环境 - 追加日志
    /// </summary>
    private void AppendTestLog(string message)
    {
        OnTestLogAppended(message);
    }

    /// <summary>
    /// 测试环境按时间条件 - 查询按钮点击
    /// </summary>
    private async void TestTimeQueryButton_Click(object sender, RoutedEventArgs e)
    {
        // 获取Token
        var token = TestTokenTextBox.Text.Trim();
        if (string.IsNullOrEmpty(token))
        {
            MessageBox.Show("请输入授权Token", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 保存Token
        _configService.SaveToken(token, EnvironmentType.Test);

        // 获取选中的状态列表
        var selectedStates = new List<int>();
        if (TestState1CheckBox.IsChecked == true) selectedStates.Add(1);
        if (TestState2CheckBox.IsChecked == true) selectedStates.Add(2);
        if (TestState4CheckBox.IsChecked == true) selectedStates.Add(4);

        if (selectedStates.Count == 0)
        {
            MessageBox.Show("请至少选择一个传输状态", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TestTimeQueryButton.IsEnabled = false;
        _testTimeRecords.Clear();

        try
        {
            var service = new DataTransmissionService();
            service.SetEnvironment(EnvironmentType.Test);
            service.SetToken(token);

            DateTime? startTime = TestTimeDatePicker.SelectedDate?.Date;
            DateTime? endTime = null;

            if (startTime != null)
            {
                AppendTestTimeLog($"[查询] 开始时间: {startTime:yyyy-MM-dd}, 结束时间: 无限制");
            }
            else
            {
                AppendTestTimeLog($"[查询] 时间范围: 全部");
            }

            AppendTestTimeLog($"[查询] 传输状态: {string.Join(", ", selectedStates.Select(s => GetStateName(s)))}");

            foreach (var state in selectedStates)
            {
                AppendTestTimeLog($"[查询] 正在查询状态={GetStateName(state)}的记录...");

                var result = await service.QueryTestByTimeAsync(startTime, endTime, state, 1, 100);
                if (result == null || !result.IsSuccess)
                {
                    AppendTestTimeLog($"[警告] 状态={GetStateName(state)}查询失败: {result?.Msg ?? "无响应"}");
                    continue;
                }

                AppendTestTimeLog($"[信息] 状态={GetStateName(state)}共 {result.Count} 条");

                if (result.Data.Count > 0)
                {
                    _testTimeRecords.AddRange(result.Data);
                }
            }

            TestTimeCountTextBlock.Text = $"记录数: {_testTimeRecords.Count} | 成功: 0 | 失败: 0";
            AppendTestTimeLog($"[完成] 共查询到 {_testTimeRecords.Count} 条记录");
        }
        catch (Exception ex)
        {
            AppendTestTimeLog($"[异常] {ex.Message}");
        }
        finally
        {
            TestTimeQueryButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// 测试环境按时间条件 - 开始循环重推
    /// </summary>
    private async void TestTimeStartButton_Click(object sender, RoutedEventArgs e)
    {
        // 获取Token
        var token = TestTokenTextBox.Text.Trim();
        if (string.IsNullOrEmpty(token))
        {
            MessageBox.Show("请输入授权Token", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 保存Token
        _configService.SaveToken(token, EnvironmentType.Test);

        // 获取选中的状态列表
        var selectedStates = new List<int>();
        if (TestState1CheckBox.IsChecked == true) selectedStates.Add(1); // 待传输
        if (TestState2CheckBox.IsChecked == true) selectedStates.Add(2); // 传输中
        if (TestState4CheckBox.IsChecked == true) selectedStates.Add(4); // 传输失败

        if (selectedStates.Count == 0)
        {
            MessageBox.Show("请至少选择一个传输状态", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_isTestTimeRunning)
        {
            MessageBox.Show("循环重推正在进行中", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _isTestTimeRunning = true;
        _testTimeCancellationTokenSource = new CancellationTokenSource();
        TestTimeStartButton.IsEnabled = false;
        TestTimeStopButton.IsEnabled = true;

        int totalSuccessCount = 0;
        int totalFailCount = 0;

        try
        {
            var service = new DataTransmissionService();
            service.SetEnvironment(EnvironmentType.Test);
            service.SetToken(token);

            // 获取开始时间
            DateTime? startTime = TestTimeDatePicker.SelectedDate?.Date;
            DateTime? endTime = null;

            if (startTime != null)
            {
                AppendTestTimeLog($"[配置] 开始时间: {startTime:yyyy-MM-dd}, 结束时间: 无限制");
            }
            else
            {
                AppendTestTimeLog($"[配置] 时间范围: 全部");
            }

            AppendTestTimeLog($"[配置] 传输状态: {string.Join(", ", selectedStates.Select(s => GetStateName(s)))}");
            AppendTestTimeLog($"[开始] 循环重推已启动...");

            while (!_testTimeCancellationTokenSource.Token.IsCancellationRequested)
            {
                // 对每个选中的状态分别查询
                _testTimeRecords.Clear();

                foreach (var state in selectedStates)
                {
                    if (_testTimeCancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    var result = await service.QueryTestByTimeAsync(startTime, endTime, state, 1, 100);

                    if (result == null || !result.IsSuccess)
                    {
                        AppendTestTimeLog($"[警告] 状态={GetStateName(state)}查询失败: {result?.Msg ?? "无响应"}");
                        continue;
                    }

                    if (result.Data.Count > 0)
                    {
                        _testTimeRecords.AddRange(result.Data);
                    }
                }

                if (_testTimeCancellationTokenSource.Token.IsCancellationRequested)
                    break;

                if (_testTimeRecords.Count == 0)
                {
                    AppendTestTimeLog($"[信息] 没有待重推数据，等待1分钟后重新扫描...");
                    TestTimeCountTextBlock.Text = $"记录数: 0 | 成功: {totalSuccessCount} | 失败: {totalFailCount}";
                    await Task.Delay(60000, _testTimeCancellationTokenSource.Token);
                    continue;
                }

                AppendTestTimeLog($"[查询] 发现 {_testTimeRecords.Count} 条待重推记录，开始重推...");
                TestTimeCountTextBlock.Text = $"记录数: {_testTimeRecords.Count} | 成功: {totalSuccessCount} | 失败: {totalFailCount}";

                foreach (var record in _testTimeRecords)
                {
                    if (_testTimeCancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    AppendTestTimeLog($"[重推] ID: {record.Id}, 状态: {record.TransferStateStr}, 创建时间: {record.CreateTime}");

                    try
                    {
                        var retryResult = await service.RetryAsync(record);

                        if (retryResult != null && retryResult.IsSuccess)
                        {
                            totalSuccessCount++;
                            AppendTestTimeLog($"[成功] ID: {record.Id} - {retryResult.Msg}");
                        }
                        else
                        {
                            totalFailCount++;
                            AppendTestTimeLog($"[失败] ID: {record.Id} - {retryResult?.Msg ?? "未知错误"}");
                        }
                    }
                    catch (Exception ex)
                    {
                        totalFailCount++;
                        AppendTestTimeLog($"[异常] ID: {record.Id} - {ex.Message}");
                    }

                    TestTimeCountTextBlock.Text = $"记录数: {_testTimeRecords.Count} | 成功: {totalSuccessCount} | 失败: {totalFailCount}";

                    await Task.Delay(1000, _testTimeCancellationTokenSource.Token);
                }

                AppendTestTimeLog($"[轮次完成] 本轮重推结束，等待继续扫描...");
            }

            AppendTestTimeLog($"[停止] 循环重推已停止 - 总成功: {totalSuccessCount}, 总失败: {totalFailCount}");
        }
        catch (OperationCanceledException)
        {
            AppendTestTimeLog($"[停止] 循环重推已取消 - 总成功: {totalSuccessCount}, 总失败: {totalFailCount}");
        }
        catch (Exception ex)
        {
            AppendTestTimeLog($"[异常] {ex.Message}");
        }
        finally
        {
            _isTestTimeRunning = false;
            TestTimeStopButton.IsEnabled = false;
            TestTimeStartButton.IsEnabled = true;
            TestTimeCountTextBlock.Text = $"记录数: 0 | 成功: {totalSuccessCount} | 失败: {totalFailCount}";
            _testTimeCancellationTokenSource?.Dispose();
            _testTimeCancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 测试环境按时间条件 - 停止重推
    /// </summary>
    private void TestTimeStopButton_Click(object sender, RoutedEventArgs e)
    {
        _testTimeCancellationTokenSource?.Cancel();
        TestTimeStopButton.IsEnabled = false;
        AppendTestTimeLog("[信息] 正在停止循环重推...");
    }

    /// <summary>
    /// 获取状态名称
    /// </summary>
    private string GetStateName(int state)
    {
        return state switch
        {
            1 => "待传输",
            2 => "传输中",
            4 => "传输失败",
            _ => $"状态{state}"
        };
    }

    /// <summary>
    /// 按时间条件重推 - 追加日志
    /// </summary>
    private void AppendTestTimeLog(string message)
    {
        Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            TestTimeLogTextBox.AppendText($"[{timestamp}] {message}\n");
            TestTimeLogTextBox.ScrollToEnd();
        });
    }

    #endregion

    #region 放弃传输记录Tab事件

    /// <summary>
    /// 放弃Tab - 查询传输中记录（今天以前）
    /// </summary>
    private async void AbandonQueryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isAbandonRunning)
        {
            MessageBox.Show("放弃操作正在进行中，请先停止", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 直接使用测试环境保存的Token
        var token = _configService.LoadToken(EnvironmentType.Test);
        if (string.IsNullOrEmpty(token))
        {
            MessageBox.Show("请先在测试环境Tab页输入并保存Token", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AbandonQueryButton.IsEnabled = false;
        _abandonRecords.Clear();

        try
        {
            var service = new DataTransmissionService();
            service.SetEnvironment(EnvironmentType.Test);
            service.SetToken(token);

            // 计算时间范围：StartTime最小时间，EndTime今天0点
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var minTime = "1900-01-01";

            AppendAbandonLog($"[查询] 正在查询传输中的记录（时间范围: {minTime} ~ {today}）...");

            // 使用时间条件查询，直接返回今天以前的记录
            var firstPageResult = await service.QueryTestTransferringRecordsAsync(minTime, today, 1, 100);
            if (firstPageResult == null || !firstPageResult.IsSuccess)
            {
                AppendAbandonLog($"[错误] 查询失败: {firstPageResult?.Msg ?? "无响应"}");
                return;
            }

            var totalCount = firstPageResult.Count;
            AppendAbandonLog($"[信息] 传输中记录总数: {totalCount}");

            if (totalCount == 0)
            {
                AbandonCountTextBlock.Text = "可放弃记录: 0";
                AbandonExecuteButton.IsEnabled = false;
                AppendAbandonLog("[完成] 没有找到可放弃的记录");
                return;
            }

            // 收集所有记录
            _abandonRecords.AddRange(firstPageResult.Data);

            // 如果超过100条，继续查询其他页
            if (totalCount > 100)
            {
                int totalPages = (int)Math.Ceiling((double)totalCount / 100);
                for (int page = 2; page <= totalPages; page++)
                {
                    var result = await service.QueryTestTransferringRecordsAsync(minTime, today, page, 100);
                    if (result != null && result.IsSuccess && result.Data.Count > 0)
                    {
                        _abandonRecords.AddRange(result.Data);
                    }
                }
            }

            AbandonCountTextBlock.Text = $"可放弃记录: {_abandonRecords.Count}";
            AbandonExecuteButton.IsEnabled = _abandonRecords.Count > 0;
            AppendAbandonLog($"[完成] 筛选出 {_abandonRecords.Count} 条可放弃记录（创建时间在今天以前）");
        }
        catch (Exception ex)
        {
            AppendAbandonLog($"[异常] {ex.Message}");
        }
        finally
        {
            AbandonQueryButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// 放弃Tab - 执行放弃
    /// </summary>
    private async void AbandonExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_abandonRecords.Count == 0)
        {
            MessageBox.Show("没有可放弃的记录，请先查询", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_isAbandonRunning)
        {
            MessageBox.Show("放弃操作正在进行中", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show($"确定要放弃 {_abandonRecords.Count} 条传输中记录吗？\n这些记录的创建时间都在今天以前。", "确认放弃", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _isAbandonRunning = true;
        _abandonCancellationTokenSource = new CancellationTokenSource();
        AbandonExecuteButton.IsEnabled = false;
        AbandonStopButton.IsEnabled = true;
        AbandonQueryButton.IsEnabled = false;

        try
        {
            // 使用测试环境保存的Token
            var token = _configService.LoadToken(EnvironmentType.Test);
            var service = new DataTransmissionService();
            service.SetEnvironment(EnvironmentType.Test);
            service.SetToken(token);

            int successCount = 0;
            int failCount = 0;

            AppendAbandonLog($"[开始] 开始执行放弃，共 {_abandonRecords.Count} 条记录...");

            foreach (var record in _abandonRecords)
            {
                if (_abandonCancellationTokenSource.Token.IsCancellationRequested)
                {
                    AppendAbandonLog("[信息] 用户取消了放弃操作");
                    break;
                }

                AppendAbandonLog($"[处理] ID: {record.Id}, 创建时间: {record.CreateTime}");

                var detailResult = await service.GetDetailAsync(record.Id);

                if (detailResult == null || !detailResult.IsSuccess || detailResult.Data == null)
                {
                    AppendAbandonLog($"[失败] ID: {record.Id} - 查询详情失败");
                    failCount++;
                    continue;
                }

                var detail = detailResult.Data;

                var abandonRequest = new AbandonRequest
                {
                    Id = detail.Id,
                    BusinessTypeCode = detail.BusinessTypeCode,
                    AppCode = detail.InApp.AccessAppCode,
                    OutAppCode = "",
                    BusinessDataJson = null
                };

                var abandonResult = await service.AbandonAsync(abandonRequest);

                if (abandonResult != null && abandonResult.IsSuccess)
                {
                    successCount++;
                    AppendAbandonLog($"[成功] ID: {record.Id} - 已放弃");
                }
                else
                {
                    failCount++;
                    AppendAbandonLog($"[失败] ID: {record.Id} - {abandonResult?.Msg ?? "放弃失败"}");
                }

                await Task.Delay(500, _abandonCancellationTokenSource.Token);
            }

            AppendAbandonLog($"[完成] 放弃执行结束 - 成功: {successCount}, 失败: {failCount}");
            AbandonCountTextBlock.Text = $"可放弃记录: 0";
            _abandonRecords.Clear();
        }
        catch (OperationCanceledException)
        {
            AppendAbandonLog("[信息] 放弃操作已取消");
        }
        catch (Exception ex)
        {
            AppendAbandonLog($"[异常] {ex.Message}");
        }
        finally
        {
            _isAbandonRunning = false;
            AbandonStopButton.IsEnabled = false;
            AbandonExecuteButton.IsEnabled = _abandonRecords.Count > 0;
            AbandonQueryButton.IsEnabled = true;
            _abandonCancellationTokenSource?.Dispose();
            _abandonCancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 放弃Tab - 停止放弃
    /// </summary>
    private void AbandonStopButton_Click(object sender, RoutedEventArgs e)
    {
        _abandonCancellationTokenSource?.Cancel();
        AbandonStopButton.IsEnabled = false;
        AppendAbandonLog("[信息] 正在停止放弃操作...");
    }

    /// <summary>
    /// 放弃Tab - 清空日志
    /// </summary>
    private void AbandonClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        AbandonLogTextBox.Clear();
    }

    /// <summary>
    /// 放弃Tab - 追加日志
    /// </summary>
    private void AppendAbandonLog(string message)
    {
        Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            AbandonLogTextBox.AppendText($"[{timestamp}] {message}\n");
            AbandonLogTextBox.ScrollToEnd();
        });
    }

    /// <summary>
    /// 按ID重推 - 追加日志
    /// </summary>
    private void AppendRetryByIdLog(string message)
    {
        Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            RetryByIdLogTextBox.AppendText($"[{timestamp}] {message}\n");
            RetryByIdLogTextBox.ScrollToEnd();
        });
    }

    /// <summary>
    /// 按客户ID查询并重推传输中记录
    /// </summary>
    private async void RetryByIdButton_Click(object sender, RoutedEventArgs e)
    {
        // 直接使用测试环境保存的Token
        var token = _configService.LoadToken(EnvironmentType.Test);
        if (string.IsNullOrEmpty(token))
        {
            MessageBox.Show("请先在测试环境Tab页输入并保存Token", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var customerId = CustomerIdTextBox.Text.Trim();
        if (string.IsNullOrEmpty(customerId))
        {
            MessageBox.Show("请输入客户ID", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        RetryByIdButton.IsEnabled = false;

        try
        {
            var service = new DataTransmissionService();
            service.SetEnvironment(EnvironmentType.Test);
            service.SetToken(token);

            AppendRetryByIdLog($"[查询] 正在查询客户ID: {customerId} 的传输中记录...");

            var result = await service.QueryTestTransferringByIdAsync(customerId, 1, 100);
            if (result == null || !result.IsSuccess)
            {
                AppendRetryByIdLog($"[错误] 查询失败: {result?.Msg ?? "无响应"}");
                return;
            }

            if (result.Count == 0)
            {
                AppendRetryByIdLog($"[信息] 未找到客户ID: {customerId} 的传输中记录");
                return;
            }

            AppendRetryByIdLog($"[信息] 找到 {result.Count} 条传输中记录，开始重推...");

            int successCount = 0;
            int failCount = 0;

            foreach (var record in result.Data)
            {
                AppendRetryByIdLog($"[重推] ID: {record.Id}, 创建时间: {record.CreateTime}");

                try
                {
                    var retryResult = await service.RetryAsync(record);

                    if (retryResult != null && retryResult.IsSuccess)
                    {
                        successCount++;
                        AppendRetryByIdLog($"[成功] ID: {record.Id} - {retryResult.Msg}");
                    }
                    else
                    {
                        failCount++;
                        AppendRetryByIdLog($"[失败] ID: {record.Id} - {retryResult?.Msg ?? "未知错误"}");
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    AppendRetryByIdLog($"[异常] ID: {record.Id} - {ex.Message}");
                }

                await Task.Delay(1000);
            }

            AppendRetryByIdLog($"[完成] 重推结束 - 成功: {successCount}, 失败: {failCount}");
        }
        catch (Exception ex)
        {
            AppendRetryByIdLog($"[异常] {ex.Message}");
        }
        finally
        {
            RetryByIdButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// 提取客户名称
    /// </summary>
    private string ExtractCustomerName(string businessDataJson)
    {
        try
        {
            dynamic? data = Newtonsoft.Json.JsonConvert.DeserializeObject(businessDataJson);
            return data?.KeyInfo?.CustomerName?.ToString() ?? "未知";
        }
        catch
        {
            return "解析失败";
        }
    }

    #endregion

    #region 共用事件

    /// <summary>
    /// 清空推送记录
    /// </summary>
    private void ClearRecordsButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("确定要清空所有推送记录吗？\n清空后所有记录将可以重新推送。", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            _databaseService.ClearAllRecords();
            MessageBox.Show("推送记录已清空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion

    protected override void OnClosed(EventArgs e)
    {
        _productionCancellationTokenSource?.Cancel();
        _testCancellationTokenSource?.Cancel();
        _testTimeCancellationTokenSource?.Cancel();
        _abandonCancellationTokenSource?.Cancel();
        _databaseService?.Dispose();
        base.OnClosed(e);
    }
}