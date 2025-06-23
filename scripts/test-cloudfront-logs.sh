#!/bin/bash
# cat logs/log.txt | awk '{if ($29 = ClientCommError) print $1" "$2" "$9" "$8" "$29}'
cat logs/log.txt | awk '{if ($29 == "ClientCommError") print $0}'