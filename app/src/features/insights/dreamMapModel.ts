import {
  FactInsightResponse,
  InsightsResponse,
  RelationshipInsightResponse,
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

export type DreamPatternRelation = {
  pattern: DreamPattern;
  count: number;
  strengthPercent: number;
  description: string;
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

export function findPattern(insights: InsightsResponse, type: string, name: string): DreamPattern {
  const group = insights.factGroups.find((candidate) => candidate.type === type);
  const fact = group?.facts.find((candidate) => sameValue(candidate.value, name));
  if (fact) return patternFromFact(type, fact);

  const patternType = toPatternType(type);
  const relationship = insights.relationships.find(
    (candidate) =>
      (candidate.firstType === type && sameValue(candidate.firstValue, name)) ||
      (candidate.secondType === type && sameValue(candidate.secondValue, name)),
  );
  const count = relationship
    ? relationship.firstType === type && sameValue(relationship.firstValue, name)
      ? relationship.firstDreams
      : relationship.secondDreams
    : 0;

  return {
    id: patternId(patternType, name),
    type: patternType,
    name,
    count,
    journalPercentage: insights.totalDreams > 0 ? Math.round((count / insights.totalDreams) * 1000) / 10 : 0,
    lastObservedAt: null,
  };
}

export function relationsForPattern(insights: InsightsResponse, selected: DreamPattern): DreamPatternRelation[] {
  return insights.relationships
    .filter((relationship) => relationshipContains(relationship, selected))
    .map((relationship) => {
      const selectedIsFirst = relationship.firstType === selected.type && sameValue(relationship.firstValue, selected.name);
      const relatedType = selectedIsFirst ? relationship.secondType : relationship.firstType;
      const relatedName = selectedIsFirst ? relationship.secondValue : relationship.firstValue;
      const strengthPercent = selected.count > 0 ? Math.round((relationship.sharedDreams / selected.count) * 100) : 0;
      return {
        pattern: findPattern(insights, relatedType, relatedName),
        count: relationship.sharedDreams,
        strengthPercent,
        description: `Appears with ${relatedName}`,
      };
    })
    .sort((left, right) => right.count - left.count);
}

export function patternTypeLabel(type: DreamPatternType) {
  return type.charAt(0).toUpperCase() + type.slice(1);
}

function relationshipContains(relationship: RelationshipInsightResponse, selected: DreamPattern) {
  return (
    (relationship.firstType === selected.type && sameValue(relationship.firstValue, selected.name)) ||
    (relationship.secondType === selected.type && sameValue(relationship.secondValue, selected.name))
  );
}

function patternId(type: DreamPatternType, name: string) {
  return `${type}:${name.trim().toLocaleLowerCase()}`;
}

function sameValue(left: string, right: string) {
  return left.localeCompare(right, undefined, { sensitivity: "accent" }) === 0;
}

function toPatternType(type: string): DreamPatternType {
  return knownTypes.has(type as DreamPatternType) ? (type as DreamPatternType) : "theme";
}
