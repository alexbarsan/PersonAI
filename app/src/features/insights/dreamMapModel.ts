import {
  FactInsightResponse,
  InsightsResponse,
} from "@/api/dto";

export type DreamPatternType =
  | "symbol"
  | "emotion"
  | "theme"
  | "person"
  | "location"
  | "object"
  | "scenario";

export type DreamPattern = {
  id: string;
  type: DreamPatternType;
  name: string;
  count: number;
  journalPercentage: number;
  lastObservedAt: string | null;
};

const knownTypes = new Set<DreamPatternType>([
  "symbol",
  "emotion",
  "theme",
  "person",
  "location",
  "object",
  "scenario",
]);

export function patternFromFact(type: string, fact: FactInsightResponse): DreamPattern {
  const patternType = toPatternType(type);
  return {
    id: patternId(patternType, fact.value),
    type: patternType,
    name: fact.value,
    count: fact.count,
    journalPercentage: fact.percentageOfDreams,
    lastObservedAt: fact.lastObservedAt,
  };
}

export function patternTypeLabel(type: DreamPatternType) {
  return type.charAt(0).toUpperCase() + type.slice(1);
}

function patternId(type: DreamPatternType, name: string) {
  return `${type}:${name.trim().toLocaleLowerCase()}`;
}

function toPatternType(type: string): DreamPatternType {
  return knownTypes.has(type as DreamPatternType) ? (type as DreamPatternType) : "theme";
}
