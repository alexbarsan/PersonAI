module.exports = {
  preset: "jest-expo",
  setupFilesAfterEnv: ["<rootDir>/src/test/setup.ts"],
  moduleNameMapper: {
    "^@/(.*)$": "<rootDir>/src/$1",
    "^react-native-get-random-values$": "<rootDir>/src/test/getRandomValuesMock.ts",
    "^msw/node$": "<rootDir>/node_modules/msw/lib/node/index.js"
  },
  testMatch: ["**/*.test.ts", "**/*.test.tsx"],
  transform: { "^.+\\.mjs$": "babel-jest" },
  transformIgnorePatterns: [
    "node_modules/(?!((jest-)?react-native|react-native-svg|lucide-react-native|@react-native|expo(nent)?|@expo(nent)?/.*|expo-.*|@expo/.*|@react-navigation/.*|@testing-library/react-native|react-native-safe-area-context)/)"
  ]
};
