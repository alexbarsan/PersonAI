const { spawnSync } = require("node:child_process");

const majorVersion = Number.parseInt(process.versions.node.split(".")[0] ?? "0", 10);
const nodeArgs = majorVersion >= 23 ? ["--no-webstorage"] : [];
const jestBin = require.resolve("jest/bin/jest");

const result = spawnSync(
  process.execPath,
  [...nodeArgs, jestBin, "--runInBand", "--forceExit"],
  { stdio: "inherit" },
);

process.exit(result.status ?? 1);
