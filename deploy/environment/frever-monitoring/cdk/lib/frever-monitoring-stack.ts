import * as cdk from "@aws-cdk/core";
import * as cloudwatch from "@aws-cdk/aws-cloudwatch";
import * as cw_actions from "@aws-cdk/aws-cloudwatch-actions";
import * as sns from "@aws-cdk/aws-sns";
import * as rds from "@aws-cdk/aws-rds";
import * as sqs from "@aws-cdk/aws-sqs";
import * as budget from "@aws-cdk/aws-budgets";
import * as guardduty from "@aws-cdk/aws-guardduty";
import * as iam from "@aws-cdk/aws-iam";
import * as events from "@aws-cdk/aws-events";
import * as targets from "@aws-cdk/aws-events-targets";
import * as logs from "@aws-cdk/aws-logs";

export class FreverMonitoringStack extends cdk.Stack {
  constructor(scope: cdk.Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const topic = new sns.Topic(this, "Alarms");

    const dashboard = new cloudwatch.Dashboard(this, "KPIs Dashboard", {
      dashboardName: "KPIs",
      end: "end",
      periodOverride: cloudwatch.PeriodOverride.AUTO,
      start: "start",
    });

    this.createDbAndSqsAlerts(topic, dashboard);
    this.createElasticacheAlerts(topic, dashboard);
    this.createMediaConvertAlerts(topic, dashboard);

    let widgets = [
      ...this.createLoadBalancerAlerts(topic),
      ...this.createEksClusterAlerts(topic),
    ];

    dashboard.addWidgets(...widgets);

    this.createBudgetAlerts(topic);
    this.createGuardDutyAlerts(topic);
    this.createEnforceMfaPolicy();
    this.createLogAlerts(topic);
  }

  private createDbAndSqsAlerts(
    topic: sns.Topic,
    dashboard: cloudwatch.Dashboard
  ) {
    const productionMainDB =
      rds.DatabaseInstance.fromDatabaseInstanceAttributes(
        this,
        "production-main",
        {
          instanceIdentifier: "production-main",
          instanceEndpointAddress: "xxxxxxxxx",
          port: 5432,
          securityGroups: [],
        }
      );

    const productionVideoDB =
      rds.DatabaseInstance.fromDatabaseInstanceAttributes(
        this,
        "production-video",
        {
          instanceIdentifier: "production-video",
          instanceEndpointAddress: "xxxxxxxxx",
          port: 5432,
          securityGroups: [],
        }
      );

    const productionAuthDB =
      rds.DatabaseInstance.fromDatabaseInstanceAttributes(
        this,
        "production-auth",
        {
          instanceIdentifier: "production-auth",
          instanceEndpointAddress: "xxxxxxxxx",
          port: 5432,
          securityGroups: [],
        }
      );

    const queueAssetCopy = sqs.Queue.fromQueueArn(
      this,
      "sqsAssetCopy",
      "xxxxxxxxx"
    );
    const queueMediaConvert = sqs.Queue.fromQueueArn(
      this,
      "sqsMediaConvert",
      "xxxxxxxxx"
    );
    const queueVideoConvert = sqs.Queue.fromQueueArn(
      this,
      "sqsVideoConvert",
      "xxxxxxxxx"
    );

    const metricsSum = [
      {
        metric: productionMainDB.metricDatabaseConnections(),
        name: "production-main",
        threshold: 1360,
        statistics: "Sum",
      },
      {
        metric: productionVideoDB.metricDatabaseConnections(),
        name: "production-video",
        threshold: 800,
        statistics: "Sum",
      },
      {
        metric: productionAuthDB.metricDatabaseConnections(),
        name: "production-auth",
        threshold: 70,
        statistics: "Sum",
      },
      {
        metric: productionMainDB.metric("ReadLatency", {
          statistic: "Average",
          period: cdk.Duration.seconds(60),
        }),
        name: "production-main",
        threshold: 0.0009,
        statistics: "Average",
      },
      {
        metric: productionMainDB.metric("WriteLatency", {
          statistic: "Average",
          period: cdk.Duration.seconds(60),
        }),
        name: "production-main",
        threshold: 0.025,
        statistics: "Average",
      },
      {
        metric: productionVideoDB.metric("ReadLatency", {
          statistic: "Average",
          period: cdk.Duration.seconds(60),
        }),
        name: "production-video",
        threshold: 0.0015,
        statistics: "Average",
      },
      {
        metric: productionVideoDB.metric("WriteLatency", {
          statistic: "Average",
          period: cdk.Duration.seconds(60),
        }),
        name: "production-video",
        threshold: 0.04,
        statistics: "Average",
      },
      {
        metric: productionAuthDB.metric("ReadLatency", {
          statistic: "Average",
          period: cdk.Duration.seconds(60),
        }),
        name: "production-auth",
        threshold: 0.0009,
        statistics: "Average",
      },
      {
        metric: productionAuthDB.metric("WriteLatency", {
          statistic: "Average",
          period: cdk.Duration.seconds(60),
        }),
        name: "production-auth",
        threshold: 0.015,
        statistics: "Average",
      },
      {
        metric: queueAssetCopy.metricNumberOfMessagesSent(),
        name: "asset-copy",
        threshold: 2000,
        statistics: "Sum",
      },
      {
        metric: queueMediaConvert.metricNumberOfMessagesSent(),
        name: "media-convert",
        threshold: 2000,
        statistics: "Sum",
      },
      {
        metric: queueVideoConvert.metricNumberOfMessagesSent(),
        name: "video-convert",
        threshold: 2000,
        statistics: "Sum",
      },
    ];

    let listOfWidgets1 = [];

    for (let [i, metric] of metricsSum.entries()) {
      const alarm = new cloudwatch.Alarm(
        this,
        metric.metric.metricName + "-" + metric.name,
        {
          metric: metric.metric,
          comparisonOperator:
            cloudwatch.ComparisonOperator.GREATER_THAN_THRESHOLD,
          threshold: metric.threshold,
          evaluationPeriods: 1,
          statistic: metric.statistics,
          actionsEnabled: true,
        }
      );

      alarm.addAlarmAction(new cw_actions.SnsAction(topic));

      listOfWidgets1.push(
        new cloudwatch.GraphWidget({
          title: metric.metric.metricName + "-" + metric.name,
          left: [metric.metric],
        })
      );

      if ((i + 1) % 4 === 0 || i === metricsSum.length - 1) {
        dashboard.addWidgets(...listOfWidgets1);
        listOfWidgets1 = [];
      }
    }
  }

  private createElasticacheAlerts(
    topic: sns.Topic,
    dashboard: cloudwatch.Dashboard
  ) {
    const elasticacheMetrics = [
      {
        name: "CPUUtilization",
        dimensionName: "CacheClusterId",
        dimensionValue: "content-prod-cache",
        threshold: 70,
      },
      {
        name: "DatabaseMemoryUsagePercentage",
        dimensionName: "CacheClusterId",
        dimensionValue: "content-prod-cache",
        threshold: 70,
      },
      {
        name: "GetTypeCmdsLatency",
        dimensionName: "CacheClusterId",
        dimensionValue: "content-prod-cache",
        threshold: 1000000,
      },
      {
        name: "SetTypeCmdsLatency",
        dimensionName: "CacheClusterId",
        dimensionValue: "content-prod-cache",
        threshold: 1000000,
      },
    ];

    let listOfWidgets2 = [];

    for (let metric of elasticacheMetrics) {
      const met = new cloudwatch.Metric({
        namespace: "AWS/ElastiCache",
        metricName: metric.name,
        dimensions: { [metric.dimensionName]: metric.dimensionValue },
      });

      const alarm = new cloudwatch.Alarm(
        this,
        met.metricName + "-" + metric.dimensionValue,
        {
          metric: met,
          comparisonOperator:
            cloudwatch.ComparisonOperator.GREATER_THAN_THRESHOLD,
          threshold: metric.threshold,
          evaluationPeriods: 1,
          statistic: "Average",
          actionsEnabled: true,
        }
      );

      alarm.addAlarmAction(new cw_actions.SnsAction(topic));

      listOfWidgets2.push(
        new cloudwatch.GraphWidget({
          title: met.metricName + "-" + metric.dimensionValue,
          left: [met],
        })
      );
    }

    dashboard.addWidgets(...listOfWidgets2);
  }

  private createLoadBalancerAlerts(topic: sns.Topic): cloudwatch.GraphWidget[] {
    const widgets = [];
    const elbContentProdMetrics = [
      {
        name: "TargetResponseTime",
        dimensionName: "LoadBalancer",
        dimensionValue: "app/content-prod/27eea2c95a05b83a",
        threshold: 5,
        comparison: cloudwatch.ComparisonOperator.GREATER_THAN_THRESHOLD,
        statistics: "Average",
      },
      {
        name: "HTTPCode_Target_5XX_Count",
        dimensionName: "LoadBalancer",
        dimensionValue: "app/content-prod/27eea2c95a05b83a",
        threshold: 1,
        comparison:
          cloudwatch.ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        statistics: "Sum",
      },
    ];

    for (let metric of elbContentProdMetrics) {
      const met = new cloudwatch.Metric({
        namespace: "AWS/ApplicationELB",
        metricName: metric.name,
        dimensions: { [metric.dimensionName]: metric.dimensionValue },
      });

      const alarm = new cloudwatch.Alarm(
        this,
        met.metricName + "-" + metric.dimensionValue,
        {
          metric: met,
          comparisonOperator: metric.comparison,
          threshold: metric.threshold,
          evaluationPeriods: 1,
          statistic: metric.statistics,
          actionsEnabled: true,
        }
      );

      alarm.addAlarmAction(new cw_actions.SnsAction(topic));

      widgets.push(
        new cloudwatch.GraphWidget({
          title: met.metricName + "-" + metric.dimensionValue,
          left: [met],
        })
      );
    }

    return widgets;
  }

  private createMediaConvertAlerts(
    topic: sns.Topic,
    dashboard: cloudwatch.Dashboard
  ) {
    const widgets = [];
    const metMediaConvert = [
      {
        name: "StandbyTime",
        dimensionName: "Queue",
        dimensionValue: "xxxxxxxxx",
        threshold: 60000,
        comparison: cloudwatch.ComparisonOperator.GREATER_THAN_THRESHOLD,
        statistics: "Sum",
      },
      {
        name: "JobsErroredCount",
        dimensionName: "Queue",
        dimensionValue: "xxxxxxxxx",
        threshold: 1,
        comparison:
          cloudwatch.ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        statistics: "Sum",
      },
    ];

    for (let metric of metMediaConvert) {
      const met = new cloudwatch.Metric({
        namespace: "AWS/MediaConvert",
        metricName: metric.name,
        dimensions: { [metric.dimensionName]: metric.dimensionValue },
      });

      const alarm = new cloudwatch.Alarm(this, met.metricName + "-mc-prod", {
        metric: met,
        comparisonOperator: metric.comparison,
        threshold: metric.threshold,
        evaluationPeriods: 1,
        statistic: metric.statistics,
        actionsEnabled: true,
      });

      alarm.addAlarmAction(new cw_actions.SnsAction(topic));

      widgets.push(
        new cloudwatch.GraphWidget({
          title: met.metricName + "-" + metric.dimensionValue,
          left: [met],
        })
      );
    }

    dashboard.addWidgets(...widgets);
  }

  private createEksClusterAlerts(topic: sns.Topic): cloudwatch.GraphWidget[] {
    const widgets = [];

    const metClusterContentProd = [
      {
        name: "node_cpu_utilization",
        dimensionName: "ClusterName",
        dimensionValue: "content-prod",
        threshold: 70,
        comparison: cloudwatch.ComparisonOperator.GREATER_THAN_THRESHOLD,
        statistics: "Average",
      },
      {
        name: "cluster_failed_node_count",
        dimensionName: "ClusterName",
        dimensionValue: "content-prod",
        threshold: 1,
        comparison:
          cloudwatch.ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        statistics: "Sum",
      },
    ];

    for (let metric of metClusterContentProd) {
      const met = new cloudwatch.Metric({
        namespace: "ContainerInsights",
        metricName: metric.name,
        dimensions: { [metric.dimensionName]: metric.dimensionValue },
      });

      const alarm = new cloudwatch.Alarm(
        this,
        met.metricName + "-" + metric.dimensionValue,
        {
          metric: met,
          comparisonOperator: metric.comparison,
          threshold: metric.threshold,
          evaluationPeriods: 1,
          statistic: metric.statistics,
          actionsEnabled: true,
        }
      );

      alarm.addAlarmAction(new cw_actions.SnsAction(topic));

      widgets.push(
        new cloudwatch.GraphWidget({
          title: met.metricName + "-" + metric.dimensionValue,
          left: [met],
        })
      );
    }

    return widgets;
  }

  private createBudgetAlerts(topic: sns.Topic) {
    new budget.CfnBudget(this as any, "monthly-budget", {
      budget: {
        budgetName: "Monthly AWS budget",
        budgetType: "COST",
        timeUnit: "MONTHLY",
        budgetLimit: { amount: 10000, unit: "USD" },
      },
      notificationsWithSubscribers: [
        {
          notification: {
            notificationType: "ACTUAL",
            comparisonOperator: "GREATER_THAN",
            threshold: 50, // percent
          },
          subscribers: [{ subscriptionType: "SNS", address: topic.topicArn }],
        },
        {
          notification: {
            notificationType: "ACTUAL",
            comparisonOperator: "GREATER_THAN",
            threshold: 100, // percent
          },
          subscribers: [{ subscriptionType: "SNS", address: topic.topicArn }],
        },
      ],
    });
  }

  private createGuardDutyAlerts(topic: sns.ITopic) {
    const guardDutyProps: guardduty.CfnDetectorProps = {
      enable: true,
      dataSources: {
        kubernetes: {
          auditLogs: {
            enable: true,
          },
        },
        s3Logs: {
          enable: false,
        },
      },
    };

    new guardduty.CfnDetector(
      this as any,
      "guard-duty-detector",
      guardDutyProps
    );

    // this will raise an event for all medium and high risk findings
    const rule = new events.Rule(this, "guard-duty-findings-rule", {
      eventPattern: {
        source: ["aws.guardduty"],
        detailType: ["GuardDuty Finding"],
        detail: {
          severity: [
            4, 4.0, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 5, 5.0, 5.1,
            5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 6, 6.0, 6.1, 6.2, 6.3, 6.4,
            6.5, 6.6, 6.7, 6.8, 6.9, 7, 7.0, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7,
            7.8, 7.9, 8, 8.0, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8, 8.9,
          ],
        },
      },
    });

    const message = events.RuleTargetInput.fromObject({
      "GuardDuty Finding": {
        Severity: `${events.EventField.fromPath("$.detail.severity")}`,
        Account_ID: `${events.EventField.fromPath("$.detail.accountId")}`,
        Finding_ID: `${events.EventField.fromPath("$.detail.id")}`,
        Finding_Type: `${events.EventField.fromPath("$.detail.type")}`,
        Region: `${events.EventField.fromPath("$.region")}`,
        Finding_description: `${events.EventField.fromPath(
          "$.detail.description"
        )}`,
      },
    });

    const topicTarget = new targets.SnsTopic(topic as any, {
      message: message as any,
    });

    rule.addTarget(topicTarget as any);
  }

  private createEnforceMfaPolicy() {
    const enforceMFAPolicy = new iam.PolicyDocument({
      statements: [
        new iam.PolicyStatement({
          resources: ["*"],
          actions: [
            "iam:GetAccountPasswordPolicy",
            "iam:ListVirtualMFADevices",
          ],
          effect: iam.Effect.ALLOW,
        }),
        new iam.PolicyStatement({
          resources: ["arn:aws:iam::*:user/${aws:username}"],
          actions: ["iam:ChangePassword", "iam:GetUser"],
          effect: iam.Effect.ALLOW,
        }),
        new iam.PolicyStatement({
          resources: ["arn:aws:iam::*:user/${aws:username}"],
          actions: [
            "iam:CreateAccessKey",
            "iam:DeleteAccessKey",
            "iam:ListAccessKeys",
            "iam:UpdateAccessKey",
          ],
          effect: iam.Effect.ALLOW,
        }),
        new iam.PolicyStatement({
          resources: ["arn:aws:iam::*:mfa/${aws:username}"],
          actions: [
            "iam:DeleteSigningCertificate",
            "iam:ListSigningCertificates",
            "iam:UpdateSigningCertificate",
            "iam:UploadSigningCertificate",
          ],
          effect: iam.Effect.ALLOW,
        }),
        new iam.PolicyStatement({
          resources: ["arn:aws:iam::*:mfa/${aws:username}"],
          actions: [
            "iam:DeleteSSHPublicKey",
            "iam:GetSSHPublicKey",
            "iam:ListSSHPublicKeys",
            "iam:UpdateSSHPublicKey",
            "iam:UploadSSHPublicKey",
          ],
          effect: iam.Effect.ALLOW,
        }),
        new iam.PolicyStatement({
          resources: ["arn:aws:iam::*:mfa/${aws:username}"],
          actions: [
            "iam:CreateServiceSpecificCredential",
            "iam:DeleteServiceSpecificCredential",
            "iam:ListServiceSpecificCredentials",
            "iam:ResetServiceSpecificCredential",
            "iam:UpdateServiceSpecificCredential",
          ],
          effect: iam.Effect.ALLOW,
        }),
        new iam.PolicyStatement({
          resources: ["arn:aws:iam::*:mfa/${aws:username}"],
          actions: ["iam:CreateVirtualMFADevice", "iam:DeleteVirtualMFADevice"],
          effect: iam.Effect.ALLOW,
        }),
        new iam.PolicyStatement({
          resources: ["arn:aws:iam::*:user/${aws:username}"],
          actions: [
            "iam:DeactivateMFADevice",
            "iam:EnableMFADevice",
            "iam:ListMFADevices",
            "iam:ResyncMFADevice",
          ],
          effect: iam.Effect.ALLOW,
        }),
        new iam.PolicyStatement({
          resources: ["*"],
          notActions: [
            "iam:CreateVirtualMFADevice",
            "iam:EnableMFADevice",
            "iam:GetUser",
            "iam:ListMFADevices",
            "iam:ListVirtualMFADevices",
            "iam:ResyncMFADevice",
            "sts:GetSessionToken",
          ],
          effect: iam.Effect.DENY,
          conditions: {
            BoolIfExists: {
              "aws:MultiFactorAuthPresent": "false",
            },
          },
        }),
      ],
    });

    new iam.ManagedPolicy(this, "mfa-policy", {
      managedPolicyName: "EnforceMFAPolicy",
      document: enforceMFAPolicy,
    });
  }

  private createLogAlerts(topic: sns.Topic) {
    // Create metric filters that will filter out logs with different severity and create a composite alarm so you won't get too many alarms
    // firing at once if there are different errors
    // This is an example for logs from log group 'arn:aws:logs:eu-central-1:722913253728:log-group:/aws/containerinsights/content-prod/application:*'
    /*const contentProdLog = logs.LogGroup.fromLogGroupArn(this, 'log', 'arn:aws:logs:eu-north-1:449145795606:log-group:/aws/lambda/axenon-demo-prod-getProducts:*');

      const metricFilters = [
        { name: 'metric-filter-warning', value: 'Warning' },
        { name: 'metric-filter-error', value: 'Error' },
        { name: 'metric-filter-critical', value: 'Critical' },
      ];

      let alarms = [];

      for (let filter of metricFilters) {
        const filt = contentProdLog.addMetricFilter(filter.name, {
          filterPattern: logs.FilterPattern.stringValue('$.logLevel', '=', filter.value),
          metricNamespace: 'ContentProdCluster',
          metricName: filter.value
        });

        const createdMetric = filt.metric();

        const alarm = new cloudwatch.Alarm(this, filter.name + '-alarm', {
          metric: createdMetric,
          comparisonOperator: cloudwatch.ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
          threshold: 1,
          evaluationPeriods: 1,
          statistic: 'Sum',
          actionsEnabled: true
        });

        alarms.push(alarm);
      }

      const alarmRule = cloudwatch.AlarmRule.anyOf(
        alarms[0],
        alarms[1],
        alarms[2],
      );

      const compositeAlarm = new cloudwatch.CompositeAlarm(this, 'composite-alarm', {
        alarmRule
      });

      compositeAlarm.addAlarmAction(new cw_actions.SnsAction(topic));*/
  }
}
