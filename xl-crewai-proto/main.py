# -*- coding: utf-8 -*-
from crewai import LLM
from crewai import Agent, Crew, Task
from crewai import Process
from crewai_tools import ScrapeWebsiteTool

SERVER = "http://localhost:11434"

MODEL_RESEARCHER = "ollama/qwen3.6:35b"
MODEL_CODER      = "ollama/qwen3.6:35b"

MAIN_TASK = """
Solve 1-D heat equation for constant material properties. The left side \
of the domain is held at constant temperature during the whole cycle, \
while the opposite end is exposed to steady air at room temperature and \
thus submited to convection and radiative losses.
"""


def main():
    print("Hello from xl-crewai-proto!")

    llm_research = LLM(model=MODEL_RESEARCHER, base_url=SERVER)
    llm_coding   = LLM(model=MODEL_CODER,      base_url=SERVER)

    researcher = fluid_mechanics_researcher(llm_research)
    coder      = numerical_software_developper(llm_coding)

    research_task = Task(
        description=MAIN_TASK,
        expected_output=(
            "A detailed report with the equations required to model the"
            " problem in LaTeX and the numerical formulation using finite"
            " volume method. You are nice to the programmer and always"
            " tell how to split the problem and give tips on what should"
            " be represented in the post-processing of results."
        ),
        agent=researcher,
    )

    coding_task = Task(
        description="Write Python script implementing the model",
        expected_output=(
            "A well written and documented script implementing the required"
            " problem. You are expected to use CasADi for AD and solution,"
            " and document code using Numpydoc style."
        ),
        agent=coder,
        contexts=[research_task]
    )

    postprocessing_task = Task(
        description="Modify the Python script to include post-processing",
        expected_output=(
            "You do not modify what has been conceived to solve the problem"
            " and add the required post-processing utilities to the model."
            " Most of the time this is a matter of using matplotlib."
        ),
        agent=coder,
        contexts=[research_task, coding_task]
    )

    crew = Crew(
        agents  = [
            researcher,
            coder
        ],
        tasks   = [
            research_task,
            coding_task,
            postprocessing_task
        ],
        process = Process.sequential,
        verbose = True,
    )

    result = crew.kickoff()

    print(result.raw)

    from IPython import embed; embed(colors="Linux")



def fluid_mechanics_researcher(llm):
    return Agent(
        role="Senior Fluid Mechanics Researcher",
        goal="Provides the mathematical formulation about a given topic",
        backstory=(
            "You're a seasoned fluid mechanics researcher with a knack "
            " for uncovering the most relevant and accurate information."
            " You're known for your thorough and well-organized research."
        ),
        llm=llm,
    )


def numerical_software_developper(llm):
    scrape_casadi = ScrapeWebsiteTool(website_url="https://web.casadi.org/docs/")

    return Agent(
        role="Python Numerical Software Developer",
        goal="You develop software for representing a given physical problem",
        backstory=(
            "You're a software developer who masters Python and has a good"
            " grasp of numerical mathematics. You're known for being the"
            " best at translating research papers into functioning apps."
        ),
        tools=[scrape_casadi],
        llm=llm,
    )


if __name__ == "__main__":
    main()
