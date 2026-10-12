import { cp, mkdir, rm } from "node:fs/promises";
import path from "node:path";

const source = path.resolve("node_modules/tom-select/dist");
const destination = path.resolve("wwwroot/lib/tom-select");

await rm(destination, { recursive: true, force: true });
await mkdir(destination, { recursive: true });

await cp(
  path.join(source, "css/tom-select.css"),
  path.join(destination, "tom-select.css"),
);
await cp(
  path.join(source, "js/tom-select.complete.min.js"),
  path.join(destination, "tom-select.complete.min.js"),
);
