const axios = require("axios").default;

exports.handler = async (event) => {
  const subject = event["Records"][0]["Sns"]["Subject"];
  let message = event["Records"][0]["Sns"]["Message"];
  if (typeof message === "string") {
    message = JSON.parse(message);
  }

  let body = {
    text: "sample",
    blocks: [
      { type: "divider" },
      {
        type: "section",
        text: {
          type: "mrkdwn",
          text: `*ALARM triggered at ${message.StateChangeTime} with subject: ${subject}*`,
        },
      },
      {
        type: "section",
        text: {
          type: "mrkdwn",
          text: message.NewStateReason,
        },
      },
      {
        type: "section",
        text: {
          type: "mrkdwn",
          text: `*${message.Trigger.Statistic}* _${message.Trigger.Namespace}/*${message.Trigger.MetricName}*_ ${message.Trigger.ComparisonOperator} ${message.Trigger.Threshold} for *${message.Trigger.Period}* secs`,
        },
      },
    ],
  };

  if (message.Trigger.Dimensions != null && typeof message.Trigger.Dimensions[Symbol.iterator] === 'function') {
    for (let dim of message.Trigger.Dimensions) {
      body.blocks.push({
        type: "section",
        text: {
          type: "mrkdwn",
          text: `${dim.name} = ${dim.value}`,
        },
      });
    }
  }

  body.blocks.push({ type: "divider" });

  await axios.post(
    "xxxxxxxxx",
    body
  );
};
